using Microsoft.Extensions.Logging.Abstractions;
using OpenMES.Data.Common;
using OpenMES.Data.Dtos;
using OpenMES.Data.Entities;
using OpenMES.WebApi.Controllers;
using Xunit;

namespace OpenMES.Tests;

/// <summary>
/// Tests for OperatorShift state transitions and CheckOut side effects.
/// </summary>
public class OperatorShiftTests
{
	private static OperatorShiftController Controller(OpenMES.Data.Contexts.OpenMESDbContext db)
		=> new(db, NullLogger<OperatorShiftController>.Instance);

	private static OperatorShiftDto ShiftDto(int opId, OperatorEventType type)
	{
		var now = DateTimeOffset.UtcNow;
		return new OperatorShiftDto
		{
			OperatorId = opId, EventType = type,
			EventTime = now, Source = "Manual",
		};
	}

	private static async Task AddEvent(
		OpenMES.Data.Contexts.OpenMESDbContext db, int opId, OperatorEventType type, int minutesAgo = 60)
	{
		var now = DateTimeOffset.UtcNow;
		db.OperatorShifts.Add(new OperatorShift
		{
			OperatorId = opId, EventType = type,
			EventTime = now.AddMinutes(-minutesAgo), Source = "Manual",
			CreatedAt = now, UpdatedAt = now,
		});
		await db.SaveChangesAsync();
	}

	// ── Valid state transitions ───────────────────────────────────────────────

	[Fact]
	public async Task CheckIn_FromAbsent_Succeeds()
	{
		var (db, _, op1, _, _) = await TestDbFactory.SeedForWorkSessionTests();
		// No prior events
		var result = await Controller(db).Create(ShiftDto(op1.Id, OperatorEventType.CheckIn), CancellationToken.None);
		Assert.True(result.Value > 0);
	}

	[Fact]
	public async Task FullShift_CheckIn_Break_BreakEnd_CheckOut_AllSucceed()
	{
		var (db, _, op1, _, _) = await TestDbFactory.SeedForWorkSessionTests();
		var ctrl = Controller(db);
		var now = DateTimeOffset.UtcNow;

		await AddEvent(db, op1.Id, OperatorEventType.CheckIn, minutesAgo: 120);
		await AddEvent(db, op1.Id, OperatorEventType.BreakStart, minutesAgo: 60);
		await AddEvent(db, op1.Id, OperatorEventType.BreakEnd, minutesAgo: 30);

		// CheckOut must succeed
		var result = await ctrl.Create(ShiftDto(op1.Id, OperatorEventType.CheckOut), CancellationToken.None);
		Assert.True(result.Value > 0);
	}

	// ── Invalid transitions: CheckIn ─────────────────────────────────────────

	[Fact]
	public async Task CheckIn_WhenAlreadyCheckedIn_ThrowsProblemException()
	{
		var (db, _, op1, _, _) = await TestDbFactory.SeedForWorkSessionTests();
		await AddEvent(db, op1.Id, OperatorEventType.CheckIn);

		await Assert.ThrowsAsync<OpenMES.WebApi.ProblemException>(
			() => Controller(db).Create(ShiftDto(op1.Id, OperatorEventType.CheckIn), CancellationToken.None));
	}

	[Fact]
	public async Task CheckIn_WhenOnBreak_ThrowsProblemException()
	{
		var (db, _, op1, _, _) = await TestDbFactory.SeedForWorkSessionTests();
		await AddEvent(db, op1.Id, OperatorEventType.CheckIn, minutesAgo: 90);
		await AddEvent(db, op1.Id, OperatorEventType.BreakStart, minutesAgo: 30);

		await Assert.ThrowsAsync<OpenMES.WebApi.ProblemException>(
			() => Controller(db).Create(ShiftDto(op1.Id, OperatorEventType.CheckIn), CancellationToken.None));
	}

	// ── Invalid transitions: CheckOut ────────────────────────────────────────

	[Fact]
	public async Task CheckOut_WithNoShift_ThrowsProblemException()
	{
		var (db, _, op1, _, _) = await TestDbFactory.SeedForWorkSessionTests();

		await Assert.ThrowsAsync<OpenMES.WebApi.ProblemException>(
			() => Controller(db).Create(ShiftDto(op1.Id, OperatorEventType.CheckOut), CancellationToken.None));
	}

	[Fact]
	public async Task CheckOut_WhenAlreadyCheckedOut_ThrowsProblemException()
	{
		var (db, _, op1, _, _) = await TestDbFactory.SeedForWorkSessionTests();
		await AddEvent(db, op1.Id, OperatorEventType.CheckIn, minutesAgo: 120);
		await AddEvent(db, op1.Id, OperatorEventType.CheckOut, minutesAgo: 30);

		await Assert.ThrowsAsync<OpenMES.WebApi.ProblemException>(
			() => Controller(db).Create(ShiftDto(op1.Id, OperatorEventType.CheckOut), CancellationToken.None));
	}

	// ── Invalid transitions: BreakStart ──────────────────────────────────────

	[Fact]
	public async Task BreakStart_WhenNotCheckedIn_ThrowsProblemException()
	{
		var (db, _, op1, _, _) = await TestDbFactory.SeedForWorkSessionTests();

		await Assert.ThrowsAsync<OpenMES.WebApi.ProblemException>(
			() => Controller(db).Create(ShiftDto(op1.Id, OperatorEventType.BreakStart), CancellationToken.None));
	}

	[Fact]
	public async Task BreakStart_WhenAlreadyOnBreak_ThrowsProblemException()
	{
		var (db, _, op1, _, _) = await TestDbFactory.SeedForWorkSessionTests();
		await AddEvent(db, op1.Id, OperatorEventType.CheckIn, minutesAgo: 90);
		await AddEvent(db, op1.Id, OperatorEventType.BreakStart, minutesAgo: 30);

		await Assert.ThrowsAsync<OpenMES.WebApi.ProblemException>(
			() => Controller(db).Create(ShiftDto(op1.Id, OperatorEventType.BreakStart), CancellationToken.None));
	}

	[Fact]
	public async Task BreakStart_AfterCheckOut_ThrowsProblemException()
	{
		var (db, _, op1, _, _) = await TestDbFactory.SeedForWorkSessionTests();
		await AddEvent(db, op1.Id, OperatorEventType.CheckIn, minutesAgo: 120);
		await AddEvent(db, op1.Id, OperatorEventType.CheckOut, minutesAgo: 30);

		await Assert.ThrowsAsync<OpenMES.WebApi.ProblemException>(
			() => Controller(db).Create(ShiftDto(op1.Id, OperatorEventType.BreakStart), CancellationToken.None));
	}

	// ── Invalid transitions: BreakEnd ────────────────────────────────────────

	[Fact]
	public async Task BreakEnd_WhenNotOnBreak_ThrowsProblemException()
	{
		var (db, _, op1, _, _) = await TestDbFactory.SeedForWorkSessionTests();
		await AddEvent(db, op1.Id, OperatorEventType.CheckIn);

		await Assert.ThrowsAsync<OpenMES.WebApi.ProblemException>(
			() => Controller(db).Create(ShiftDto(op1.Id, OperatorEventType.BreakEnd), CancellationToken.None));
	}

	[Fact]
	public async Task BreakEnd_WhenCheckedOut_ThrowsProblemException()
	{
		var (db, _, op1, _, _) = await TestDbFactory.SeedForWorkSessionTests();
		await AddEvent(db, op1.Id, OperatorEventType.CheckIn, minutesAgo: 120);
		await AddEvent(db, op1.Id, OperatorEventType.CheckOut, minutesAgo: 30);

		await Assert.ThrowsAsync<OpenMES.WebApi.ProblemException>(
			() => Controller(db).Create(ShiftDto(op1.Id, OperatorEventType.BreakEnd), CancellationToken.None));
	}

	[Fact]
	public async Task BreakEnd_WithNoHistory_ThrowsProblemException()
	{
		var (db, _, op1, _, _) = await TestDbFactory.SeedForWorkSessionTests();

		await Assert.ThrowsAsync<OpenMES.WebApi.ProblemException>(
			() => Controller(db).Create(ShiftDto(op1.Id, OperatorEventType.BreakEnd), CancellationToken.None));
	}

	// ── CheckOut side effects ─────────────────────────────────────────────────

	[Fact]
	public async Task CheckOut_WithSessionsOnMultiplePhases_ClosesAllSessions()
	{
		var (db, machine, op1, _, phase1) = await TestDbFactory.SeedForWorkSessionTests();
		await AddEvent(db, op1.Id, OperatorEventType.CheckIn, minutesAgo: 180);

		// Create a second phase on the same order
		var phase2 = new ProductionOrderPhase
		{
			ProductionOrderId = phase1.ProductionOrderId,
			PhaseNumber = "0020",
			WorkCenterId = phase1.WorkCenterId,
			ExternalId = "EXT-0020",
			PlannedQuantity = 50,
			Status = OrderStatus.Released,
			WorkCode = "MILL", Description = "Milling",
		};
		db.ProductionOrderPhases.Add(phase2);
		await db.SaveChangesAsync();

		// Op1 has one open session on each phase
		var now = DateTimeOffset.UtcNow;
		var s1 = new WorkSession
		{
			OperatorId = op1.Id, ProductionOrderPhaseId = phase1.Id, MachineId = machine.Id,
			SessionType = WorkSessionType.Work, Status = WorkSessionStatus.Open,
			StartTime = now.AddMinutes(-60), Source = "Manual",
			CreatedAt = now, UpdatedAt = now,
		};
		var s2 = new WorkSession
		{
			OperatorId = op1.Id, ProductionOrderPhaseId = phase2.Id, MachineId = machine.Id,
			SessionType = WorkSessionType.Setup, Status = WorkSessionStatus.Open,
			StartTime = now.AddMinutes(-30), Source = "Manual",
			CreatedAt = now, UpdatedAt = now,
		};
		db.WorkSessions.AddRange(s1, s2);
		await db.SaveChangesAsync();

		await Controller(db).Create(ShiftDto(op1.Id, OperatorEventType.CheckOut), CancellationToken.None);

		Assert.Equal(WorkSessionStatus.Closed, (await db.WorkSessions.FindAsync(s1.Id))!.Status);
		Assert.Equal(WorkSessionStatus.Closed, (await db.WorkSessions.FindAsync(s2.Id))!.Status);
		Assert.NotNull((await db.WorkSessions.FindAsync(s1.Id))!.EndTime);
		Assert.NotNull((await db.WorkSessions.FindAsync(s2.Id))!.EndTime);
	}

	[Fact]
	public async Task CheckOut_WithNoOpenSessions_Succeeds()
	{
		var (db, _, op1, _, _) = await TestDbFactory.SeedForWorkSessionTests();
		await AddEvent(db, op1.Id, OperatorEventType.CheckIn, minutesAgo: 60);
		// No sessions added

		var result = await Controller(db).Create(ShiftDto(op1.Id, OperatorEventType.CheckOut), CancellationToken.None);

		Assert.True(result.Value > 0);
	}

	[Fact]
	public async Task CheckOut_DoesNotCloseSessionsOfOtherOperators()
	{
		var (db, machine, op1, op2, phase) = await TestDbFactory.SeedForWorkSessionTests();
		await AddEvent(db, op1.Id, OperatorEventType.CheckIn, minutesAgo: 90);

		// Op2 also has an open session — it must NOT be closed by op1's checkout
		var now = DateTimeOffset.UtcNow;
		var s_op2 = new WorkSession
		{
			OperatorId = op2.Id, ProductionOrderPhaseId = phase.Id, MachineId = machine.Id,
			SessionType = WorkSessionType.Work, Status = WorkSessionStatus.Open,
			StartTime = now.AddMinutes(-60), Source = "Manual",
			CreatedAt = now, UpdatedAt = now,
		};
		db.WorkSessions.Add(s_op2);
		await db.SaveChangesAsync();

		await Controller(db).Create(ShiftDto(op1.Id, OperatorEventType.CheckOut), CancellationToken.None);

		// Op2's session must remain Open
		var op2Session = await db.WorkSessions.FindAsync(s_op2.Id);
		Assert.Equal(WorkSessionStatus.Open, op2Session!.Status);
	}

	// ── Mixed session types: Setup + Work on same machine ────────────────────

	[Fact]
	public async Task Uniform_SetupAndWork_SameMachine_BothCountedInAllocation()
	{
		var (db, machine, op1, op2, phase) = await TestDbFactory.SeedForWorkSessionTests(
			MachineTimeAllocationMode.Uniform, allowConcurrent: true);

		var now = DateTimeOffset.UtcNow;

		// op1: 30 min Setup, op2: 30 min Work → total 60, 2 distinct ops → 30 each
		var setup = new WorkSession
		{
			OperatorId = op1.Id, ProductionOrderPhaseId = phase.Id, MachineId = machine.Id,
			SessionType = WorkSessionType.Setup, Status = WorkSessionStatus.Open,
			StartTime = now.AddMinutes(-30), Source = "Manual", CreatedAt = now, UpdatedAt = now,
		};
		var work = new WorkSession
		{
			OperatorId = op2.Id, ProductionOrderPhaseId = phase.Id, MachineId = machine.Id,
			SessionType = WorkSessionType.Work, Status = WorkSessionStatus.Open,
			StartTime = now.AddMinutes(-30), Source = "Manual", CreatedAt = now, UpdatedAt = now,
		};
		db.WorkSessions.AddRange(setup, work);
		await db.SaveChangesAsync();

		var ctrl = new WorkSessionController(db, NullLogger<WorkSessionController>.Instance);
		await ctrl.Close(setup.Id, CancellationToken.None);
		await ctrl.Close(work.Id, CancellationToken.None);

		var f_setup = await db.WorkSessions.FindAsync(setup.Id);
		var f_work  = await db.WorkSessions.FindAsync(work.Id);

		// Uniform: 60 total / 2 operators = 30 each regardless of session type
		Assert.Equal(30m, f_setup!.AllocatedMinutes, precision: 0);
		Assert.Equal(30m, f_work!.AllocatedMinutes,  precision: 0);
	}
}
