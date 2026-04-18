
using Microsoft.Extensions.Logging.Abstractions;
using OpenMES.Data.Common;
using OpenMES.Data.Entities;
using OpenMES.WebApi.Controllers;
using Xunit;

namespace OpenMES.Tests;

/// <summary>
/// Integration tests for WorkSession time allocation logic.
/// Tests both Uniform and Proportional modes, with edge cases.
/// </summary>
public class WorkSessionTimeAllocationTests
{
	// ── Helpers ───────────────────────────────────────────────────────────────

	private static WorkSessionController CreateController(
		OpenMES.Data.Contexts.OpenMESDbContext db)
		=> new(db, NullLogger<WorkSessionController>.Instance);

	private static WorkSession OpenSession(
		int operatorId, int phaseId, int machineId,
		WorkSessionType type, DateTimeOffset start)
		=> new()
		{
			OperatorId             = operatorId,
			ProductionOrderPhaseId = phaseId,
			MachineId              = machineId,
			SessionType            = type,
			Status                 = WorkSessionStatus.Open,
			StartTime              = start,
			Source                 = "Manual",
		};

	// ── Uniform allocation ────────────────────────────────────────────────────

	[Fact]
	public async Task Uniform_SingleOperator_AllocatedMinutesEqualsRawMinutes()
	{
		var (db, machine, op1, _, phase) = await TestDbFactory.SeedForWorkSessionTests(
			MachineTimeAllocationMode.Uniform);

		var start = DateTimeOffset.UtcNow.AddMinutes(-60);
		var session = OpenSession(op1.Id, phase.Id, machine.Id, WorkSessionType.Work, start);
		db.WorkSessions.Add(session);
		await db.SaveChangesAsync();

		var controller = CreateController(db);
		await controller.Close(session.Id, CancellationToken.None);

		var updated = await db.WorkSessions.FindAsync(session.Id);
		Assert.NotNull(updated);
		Assert.Equal(WorkSessionStatus.Closed, updated!.Status);
		// Uniform, 1 operator: total / 1 distinct = total raw minutes
		Assert.Equal(60m, updated.AllocatedMinutes, precision: 0);
	}

	[Fact]
	public async Task Uniform_TwoOperators_SamePhase_AllocatedMinutesEquallyDistributed()
	{
		var (db, machine, op1, op2, phase) = await TestDbFactory.SeedForWorkSessionTests(
			MachineTimeAllocationMode.Uniform, allowConcurrent: true);

		var now = DateTimeOffset.UtcNow;

		// Op1: 60 min, Op2: 30 min → total 90, 2 distinct → each gets 45
		var s1 = OpenSession(op1.Id, phase.Id, machine.Id, WorkSessionType.Work, now.AddMinutes(-60));
		var s2 = OpenSession(op2.Id, phase.Id, machine.Id, WorkSessionType.Work, now.AddMinutes(-30));
		db.WorkSessions.AddRange(s1, s2);
		await db.SaveChangesAsync();

		var controller = CreateController(db);
		await controller.Close(s1.Id, CancellationToken.None);
		await controller.Close(s2.Id, CancellationToken.None);

		var updated1 = await db.WorkSessions.FindAsync(s1.Id);
		var updated2 = await db.WorkSessions.FindAsync(s2.Id);

		// Uniform: 90 total / 2 operators = 45 each
		Assert.Equal(45m, updated1!.AllocatedMinutes, precision: 0);
		Assert.Equal(45m, updated2!.AllocatedMinutes, precision: 0);
	}

    [Fact]
    public async Task Uniform_SingleOperator_MultipleSessions_AllocatedMinutesDistributedProportionally()
    {
        var (db, machine, op1, _, phase) = await TestDbFactory.SeedForWorkSessionTests(
            MachineTimeAllocationMode.Uniform, allowConcurrent: true);

        var now = DateTimeOffset.UtcNow;

        // Op1 has two sessions: 60 min and 30 min. Total = 90 min.
        // Since there is only 1 operator, their total allocation is the full 90 min.
        // This 90 min share must be distributed proportionally across their sessions.
        var s1 = OpenSession(op1.Id, phase.Id, machine.Id, WorkSessionType.Work, now.AddMinutes(-90)); // 60 min duration if closed at -30
        var s2 = OpenSession(op1.Id, phase.Id, machine.Id, WorkSessionType.Work, now.AddMinutes(-30)); // 30 min duration if closed now

        db.WorkSessions.AddRange(s1, s2);
        await db.SaveChangesAsync();

		// Close s1 at T-30 to give it a 60-min duration
        s1.Status = WorkSessionStatus.Closed;
        s1.EndTime = now.AddMinutes(-30);

        var controller = CreateController(db);
        await controller.Close(s2.Id, CancellationToken.None); // This closes s2 and triggers reallocation for all on the phase

        var updated1 = await db.WorkSessions.FindAsync(s1.Id);
        var updated2 = await db.WorkSessions.FindAsync(s2.Id);

        // Total minutes = 90. 1 Operator. Share = 90.
        // s1 proportion = 60/90. Allocation = 90 * (60/90) = 60
        // s2 proportion = 30/90. Allocation = 90 * (30/90) = 30
        Assert.Equal(60m, updated1!.AllocatedMinutes, precision: 0);
        Assert.Equal(30m, updated2!.AllocatedMinutes, precision: 0);
    }

	// ── Proportional allocation ───────────────────────────────────────────────

	[Fact]
	public async Task Proportional_TwoOperators_AllocatedMinutesProportionalToRawDuration()
	{
		var (db, machine, op1, op2, phase) = await TestDbFactory.SeedForWorkSessionTests(
			MachineTimeAllocationMode.Proportional, allowConcurrent: true);

		var now = DateTimeOffset.UtcNow;

		// Op1: 60 min, Op2: 30 min → proportional: op1 gets 60, op2 gets 30
		var s1 = OpenSession(op1.Id, phase.Id, machine.Id, WorkSessionType.Work, now.AddMinutes(-60));
		var s2 = OpenSession(op2.Id, phase.Id, machine.Id, WorkSessionType.Work, now.AddMinutes(-30));
		db.WorkSessions.AddRange(s1, s2);
		await db.SaveChangesAsync();

		var controller = CreateController(db);
		await controller.Close(s1.Id, CancellationToken.None);
		await controller.Close(s2.Id, CancellationToken.None);

		var updated1 = await db.WorkSessions.FindAsync(s1.Id);
		var updated2 = await db.WorkSessions.FindAsync(s2.Id);

		// Proportional: each gets their own raw duration
		Assert.Equal(60m, updated1!.AllocatedMinutes, precision: 0);
		Assert.Equal(30m, updated2!.AllocatedMinutes, precision: 0);
	}

	// ── Reallocation on subsequent close ─────────────────────────────────────

	[Fact]
	public async Task Uniform_Reallocation_UpdatesAllSessionsOnPhaseOnClose()
	{
		var (db, machine, op1, op2, phase) = await TestDbFactory.SeedForWorkSessionTests(
			MachineTimeAllocationMode.Uniform, allowConcurrent: true);

		var now = DateTimeOffset.UtcNow;

		var s1 = OpenSession(op1.Id, phase.Id, machine.Id, WorkSessionType.Work, now.AddMinutes(-60));
		var s2 = OpenSession(op2.Id, phase.Id, machine.Id, WorkSessionType.Work, now.AddMinutes(-60));
		db.WorkSessions.AddRange(s1, s2);
		await db.SaveChangesAsync();

		var controller = CreateController(db);

		// Close s1 first — only one closed session, all 60 min go to op1
		await controller.Close(s1.Id, CancellationToken.None);
		var after_s1_close = await db.WorkSessions.FindAsync(s1.Id);
		Assert.Equal(60m, after_s1_close!.AllocatedMinutes, precision: 0);

		// Close s2 — now 2 distinct operators, 120 total / 2 = 60 each
		await controller.Close(s2.Id, CancellationToken.None);
		var final_s1 = await db.WorkSessions.FindAsync(s1.Id);
		var final_s2 = await db.WorkSessions.FindAsync(s2.Id);

		Assert.Equal(60m, final_s1!.AllocatedMinutes, precision: 0);
		Assert.Equal(60m, final_s2!.AllocatedMinutes, precision: 0);
	}

	// ── Force-close on CheckOut ───────────────────────────────────────────────

	[Fact]
	public async Task CheckOut_ForceClosesAllOpenSessions()
	{
		var (db, machine, op1, _, phase) = await TestDbFactory.SeedForWorkSessionTests();

		var now = DateTimeOffset.UtcNow;

		// Add CheckIn first
		db.OperatorShifts.Add(new OperatorShift
		{
			OperatorId = op1.Id,
			EventType  = OperatorEventType.CheckIn,
			EventTime  = now.AddHours(-2),
			Source     = "Manual",
		});

		var s1 = OpenSession(op1.Id, phase.Id, machine.Id, WorkSessionType.Work, now.AddMinutes(-60));
		var s2 = OpenSession(op1.Id, phase.Id, machine.Id, WorkSessionType.Setup, now.AddMinutes(-30));
		db.WorkSessions.AddRange(s1, s2);
		await db.SaveChangesAsync();

		// CheckOut via the public HTTP endpoint (calls CreateAsync internally)
		var shiftController = new OperatorShiftController(db, NullLogger<OperatorShiftController>.Instance);
		var checkOut = new OpenMES.Data.Dtos.OperatorShiftDto
		{
			OperatorId = op1.Id,
			EventType  = OperatorEventType.CheckOut,
			EventTime  = now,
			Source     = "Manual",
		};
		await shiftController.Create(checkOut, CancellationToken.None);

		// Both sessions must be closed
		var final_s1 = await db.WorkSessions.FindAsync(s1.Id);
		var final_s2 = await db.WorkSessions.FindAsync(s2.Id);

		Assert.Equal(WorkSessionStatus.Closed, final_s1!.Status);
		Assert.Equal(WorkSessionStatus.Closed, final_s2!.Status);
		Assert.NotNull(final_s1.EndTime);
		Assert.NotNull(final_s2.EndTime);
	}

	// ── Edge case: session with zero duration ─────────────────────────────────

	[Fact]
	public async Task Close_ZeroDurationSession_AllocatedMinutesIsZero()
	{
		var (db, machine, op1, _, phase) = await TestDbFactory.SeedForWorkSessionTests();

		var now = DateTimeOffset.UtcNow;
		var session = OpenSession(op1.Id, phase.Id, machine.Id, WorkSessionType.Work, now);
		db.WorkSessions.Add(session);
		await db.SaveChangesAsync();

		var controller = CreateController(db);
		await controller.Close(session.Id, CancellationToken.None);

		var updated = await db.WorkSessions.FindAsync(session.Id);
		Assert.Equal(0m, updated!.AllocatedMinutes, precision: 3);
	}

	// ── Case 1: same operator, same phase, two different machines (same work center) ──

	[Fact]
	public async Task SamePhase_TwoDifferentMachines_AllocationIsIndependentPerMachine()
	{
		// Scenario: op1 works 60 min on M1 and 30 min on M2 for the same phase.
		// M1: Uniform — only op1 → gets 60 min
		// M2: Uniform — only op1 → gets 30 min
		// Allocation must NOT bleed between machines.

		var (db, machine1, op1, _, phase) = await TestDbFactory.SeedForWorkSessionTests(
			MachineTimeAllocationMode.Uniform, allowConcurrent: true);

		// Add a second machine in the same work center
		var machine2 = new Machine
		{
			Code = "M2", Description = "Machine 2", Type = "CNC",
			Status = MachineStatus.Running,
			WorkCenterId = machine1.WorkCenterId,
			AllowConcurrentSessions = true,
			TimeAllocationMode = MachineTimeAllocationMode.Uniform,
		};
		db.Machines.Add(machine2);
		await db.SaveChangesAsync();

		var now = DateTimeOffset.UtcNow;

		var s_m1 = OpenSession(op1.Id, phase.Id, machine1.Id, WorkSessionType.Work, now.AddMinutes(-60));
		var s_m2 = OpenSession(op1.Id, phase.Id, machine2.Id, WorkSessionType.Setup, now.AddMinutes(-30));
		db.WorkSessions.AddRange(s_m1, s_m2);
		await db.SaveChangesAsync();

		var controller = CreateController(db);
		await controller.Close(s_m1.Id, CancellationToken.None);
		await controller.Close(s_m2.Id, CancellationToken.None);

		var final_m1 = await db.WorkSessions.FindAsync(s_m1.Id);
		var final_m2 = await db.WorkSessions.FindAsync(s_m2.Id);

		// M1: only s_m1 → 1 operator, 60 min / 1 = 60
		Assert.Equal(60m, final_m1!.AllocatedMinutes, precision: 0);

		// M2: only s_m2 → 1 operator, 30 min / 1 = 30
		// Allocation must NOT include s_m1 (different machine)
		Assert.Equal(30m, final_m2!.AllocatedMinutes, precision: 0);
	}

	[Fact]
	public async Task SamePhase_TwoDifferentMachines_ProportionalAllocationIsIndependent()
	{
		// Same as above but Proportional mode — result should be the same
		// since single-operator proportional = raw duration.
		var (db, machine1, op1, op2, phase) = await TestDbFactory.SeedForWorkSessionTests(
			MachineTimeAllocationMode.Proportional, allowConcurrent: true);

		var machine2 = new Machine
		{
			Code = "M2", Description = "Machine 2", Type = "CNC",
			Status = MachineStatus.Running,
			WorkCenterId = machine1.WorkCenterId,
			AllowConcurrentSessions = true,
			TimeAllocationMode = MachineTimeAllocationMode.Proportional,
		};
		db.Machines.Add(machine2);
		await db.SaveChangesAsync();

		var now = DateTimeOffset.UtcNow;

		// op1 on M1: 60 min Work
		// op2 on M1: 30 min Work   → M1 total: op1=60, op2=30 (proportional)
		// op1 on M2: 45 min Setup  → M2 independent: op1=45
		var s_op1_m1 = OpenSession(op1.Id, phase.Id, machine1.Id, WorkSessionType.Work,  now.AddMinutes(-60));
		var s_op2_m1 = OpenSession(op2.Id, phase.Id, machine1.Id, WorkSessionType.Work,  now.AddMinutes(-30));
		var s_op1_m2 = OpenSession(op1.Id, phase.Id, machine2.Id, WorkSessionType.Setup, now.AddMinutes(-45));
		db.WorkSessions.AddRange(s_op1_m1, s_op2_m1, s_op1_m2);
		await db.SaveChangesAsync();

		var controller = CreateController(db);
		await controller.Close(s_op1_m1.Id, CancellationToken.None);
		await controller.Close(s_op2_m1.Id, CancellationToken.None);
		await controller.Close(s_op1_m2.Id, CancellationToken.None);

		var f_op1_m1 = await db.WorkSessions.FindAsync(s_op1_m1.Id);
		var f_op2_m1 = await db.WorkSessions.FindAsync(s_op2_m1.Id);
		var f_op1_m2 = await db.WorkSessions.FindAsync(s_op1_m2.Id);

		// M1 proportional: op1 gets 60, op2 gets 30
		Assert.Equal(60m, f_op1_m1!.AllocatedMinutes, precision: 0);
		Assert.Equal(30m, f_op2_m1!.AllocatedMinutes, precision: 0);

		// M2 independent: op1 gets 45 (not contaminated by M1 sessions)
		Assert.Equal(45m, f_op1_m2!.AllocatedMinutes, precision: 0);
	}

	// ── Case 2: two operators, same phase, same machine ───────────────────────

	[Fact]
	public async Task TwoOperators_SameMachine_ConcurrentAllowed_BothSessionsCoexist()
	{
		// AllowConcurrentSessions = true: op1 and op2 can both be active simultaneously.
		// Both sessions must remain Open until explicitly closed.
		// After closing: Uniform → total / 2 distinct operators.
		var (db, machine, op1, op2, phase) = await TestDbFactory.SeedForWorkSessionTests(
			MachineTimeAllocationMode.Uniform, allowConcurrent: true);

		var now = DateTimeOffset.UtcNow;
		var s1 = OpenSession(op1.Id, phase.Id, machine.Id, WorkSessionType.Work, now.AddMinutes(-60));
		var s2 = OpenSession(op2.Id, phase.Id, machine.Id, WorkSessionType.Work, now.AddMinutes(-60));
		db.WorkSessions.AddRange(s1, s2);
		await db.SaveChangesAsync();

		// Both sessions must be open before any close
		Assert.Equal(WorkSessionStatus.Open, (await db.WorkSessions.FindAsync(s1.Id))!.Status);
		Assert.Equal(WorkSessionStatus.Open, (await db.WorkSessions.FindAsync(s2.Id))!.Status);

		var controller = CreateController(db);
		await controller.Close(s1.Id, CancellationToken.None);
		await controller.Close(s2.Id, CancellationToken.None);

		var f1 = await db.WorkSessions.FindAsync(s1.Id);
		var f2 = await db.WorkSessions.FindAsync(s2.Id);

		// Both closed, Uniform: 120 total / 2 distinct = 60 each
		Assert.Equal(WorkSessionStatus.Closed, f1!.Status);
		Assert.Equal(WorkSessionStatus.Closed, f2!.Status);
		Assert.Equal(60m, f1.AllocatedMinutes, precision: 0);
		Assert.Equal(60m, f2.AllocatedMinutes, precision: 0);
	}

	[Fact]
	public async Task TwoOperators_SameMachine_ConcurrentNotAllowed_SecondOpenAutoClosesFirst()
	{
		// AllowConcurrentSessions = false: opening a second session auto-closes the first.
		// Sequence: op1 opens → op2 opens via /open endpoint → op1's session must be Closed.
		var (db, machine, op1, op2, phase) = await TestDbFactory.SeedForWorkSessionTests(
			MachineTimeAllocationMode.Uniform, allowConcurrent: false);

		// First, add CheckIn events so operators pass presence validation
		var now = DateTimeOffset.UtcNow;
		db.OperatorShifts.AddRange(
			new OperatorShift { OperatorId = op1.Id, EventType = OperatorEventType.CheckIn,
				EventTime = now.AddHours(-2), Source = "Manual", CreatedAt = now, UpdatedAt = now },
			new OperatorShift { OperatorId = op2.Id, EventType = OperatorEventType.CheckIn,
				EventTime = now.AddHours(-2), Source = "Manual", CreatedAt = now, UpdatedAt = now }
		);
		// Set machine state to Running so validation passes
		db.MachineStates.Add(new MachineState
		{
			MachineId = machine.Id, Status = MachineStatus.Running,
			EventTime = now.AddHours(-3), Source = "Manual",
			CreatedAt = now, UpdatedAt = now
		});
		await db.SaveChangesAsync();

		var controller = CreateController(db);

		// Op1 opens a session via the /open endpoint
		var dto_op1 = new OpenMES.Data.Dtos.WorkSessionDto
		{
			OperatorId             = op1.Id,
			ProductionOrderPhaseId = phase.Id,
			MachineId              = machine.Id,
			SessionType            = WorkSessionType.Work,
			StartTime              = now.AddMinutes(-60),
			Source                 = "Manual",
		};
		var result_op1 = await controller.Open(dto_op1, CancellationToken.None);
		var opened_op1 = (result_op1.Result as Microsoft.AspNetCore.Mvc.OkObjectResult)?.Value
			as OpenMES.Data.Dtos.WorkSessionDto;
		Assert.NotNull(opened_op1);

		// Op2 opens a session on the SAME machine — should auto-close op1's session
		var dto_op2 = new OpenMES.Data.Dtos.WorkSessionDto
		{
			OperatorId             = op2.Id,
			ProductionOrderPhaseId = phase.Id,
			MachineId              = machine.Id,
			SessionType            = WorkSessionType.Work,
			StartTime              = now.AddMinutes(-30),
			Source                 = "Manual",
		};
		var result_op2 = await controller.Open(dto_op2, CancellationToken.None);
		var opened_op2 = (result_op2.Result as Microsoft.AspNetCore.Mvc.OkObjectResult)?.Value
			as OpenMES.Data.Dtos.WorkSessionDto;
		Assert.NotNull(opened_op2);

		// Op1's session must now be auto-closed
		var s_op1 = await db.WorkSessions.FindAsync(opened_op1!.Id);
		Assert.Equal(WorkSessionStatus.Closed, s_op1!.Status);
		Assert.NotNull(s_op1.EndTime);

		// Op2's session must still be open
		var s_op2 = await db.WorkSessions.FindAsync(opened_op2!.Id);
		Assert.Equal(WorkSessionStatus.Open, s_op2!.Status);
	}

	[Fact]
	public async Task TwoOperators_SameMachine_ConcurrentNotAllowed_AllocatedMinutesAfterBothClose()
	{
		// Scenario with AllowConcurrentSessions = false:
		// - op1 opens at T-60min
		// - op2 opens at T-30min → op1 is auto-closed with EndTime = T-30min
		//   so op1 raw duration = 30 min (T-60 to T-30)
		// - op2 closes now → op2 raw duration = 30 min (T-30 to T)
		// Uniform: total = 30 + 30 = 60 min, 2 distinct operators → 30 each
		var (db, machine, op1, op2, phase) = await TestDbFactory.SeedForWorkSessionTests(
			MachineTimeAllocationMode.Uniform, allowConcurrent: false);

		var now = DateTimeOffset.UtcNow;
		db.OperatorShifts.AddRange(
			new OperatorShift { OperatorId = op1.Id, EventType = OperatorEventType.CheckIn,
				EventTime = now.AddHours(-2), Source = "Manual", CreatedAt = now, UpdatedAt = now },
			new OperatorShift { OperatorId = op2.Id, EventType = OperatorEventType.CheckIn,
				EventTime = now.AddHours(-2), Source = "Manual", CreatedAt = now, UpdatedAt = now }
		);
		db.MachineStates.Add(new MachineState
		{
			MachineId = machine.Id, Status = MachineStatus.Running,
			EventTime = now.AddHours(-3), Source = "Manual",
			CreatedAt = now, UpdatedAt = now
		});
		await db.SaveChangesAsync();

		var controller = CreateController(db);

		// Op1 opens at T-60min
		var r1 = await controller.Open(new OpenMES.Data.Dtos.WorkSessionDto
		{
			OperatorId = op1.Id, ProductionOrderPhaseId = phase.Id, MachineId = machine.Id,
			SessionType = WorkSessionType.Work, StartTime = now.AddMinutes(-60), Source = "Manual",
		}, CancellationToken.None);
		var s1Id = ((r1.Result as Microsoft.AspNetCore.Mvc.OkObjectResult)!
			.Value as OpenMES.Data.Dtos.WorkSessionDto)!.Id;

		// Op2 opens at T-30min → auto-closes op1 with EndTime = T-30min (op1 raw = 30 min)
		var startOp2 = now.AddMinutes(-30);
		var r2 = await controller.Open(new OpenMES.Data.Dtos.WorkSessionDto
		{
			OperatorId = op2.Id, ProductionOrderPhaseId = phase.Id, MachineId = machine.Id,
			SessionType = WorkSessionType.Work, StartTime = startOp2, Source = "Manual",
		}, CancellationToken.None);
		var s2Id = ((r2.Result as Microsoft.AspNetCore.Mvc.OkObjectResult)!
			.Value as OpenMES.Data.Dtos.WorkSessionDto)!.Id;

		// Op2 closes now → op2 raw = 30 min
		await controller.Close(s2Id, CancellationToken.None);

		var f1 = await db.WorkSessions.FindAsync(s1Id);
		var f2 = await db.WorkSessions.FindAsync(s2Id);

		Assert.Equal(WorkSessionStatus.Closed, f1!.Status);
		Assert.Equal(WorkSessionStatus.Closed, f2!.Status);

		// Uniform: op1 raw = 30 min, op2 raw = 30 min, total = 60, 2 distinct operators → 30 each
		Assert.Equal(30m, f1.AllocatedMinutes, precision: 0);
		Assert.Equal(30m, f2.AllocatedMinutes, precision: 0);
	}
}
