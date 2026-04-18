using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using OpenMES.Data.Common;
using OpenMES.Data.Dtos;
using OpenMES.Data.Entities;
using OpenMES.WebApi.Controllers;
using Xunit;

namespace OpenMES.Tests;

/// <summary>
/// Tests for the /open endpoint business rules (rules 1–6) and edge cases.
/// </summary>
public class WorkSessionOpenValidationTests
{
	private static WorkSessionController Controller(OpenMES.Data.Contexts.OpenMESDbContext db)
		=> new(db, NullLogger<WorkSessionController>.Instance);

	private static WorkSessionDto OpenDto(int opId, int phaseId, int machineId,
		WorkSessionType type = WorkSessionType.Work, DateTimeOffset? start = null)
		=> new()
		{
			OperatorId             = opId,
			ProductionOrderPhaseId = phaseId,
			MachineId              = machineId,
			SessionType            = type,
			StartTime              = start ?? DateTimeOffset.UtcNow,
			Source                 = "Manual",
		};

	private static async Task AddCheckIn(OpenMES.Data.Contexts.OpenMESDbContext db, int opId)
	{
		var now = DateTimeOffset.UtcNow;
		db.OperatorShifts.Add(new OperatorShift
		{
			OperatorId = opId, EventType = OperatorEventType.CheckIn,
			EventTime = now.AddHours(-1), Source = "Manual",
			CreatedAt = now, UpdatedAt = now,
		});
		await db.SaveChangesAsync();
	}

	private static async Task SetMachineStatus(
		OpenMES.Data.Contexts.OpenMESDbContext db, int machineId, MachineStatus status)
	{
		var now = DateTimeOffset.UtcNow;
		db.MachineStates.Add(new MachineState
		{
			MachineId = machineId, Status = status,
			EventTime = now.AddMinutes(-5), Source = "Manual",
			CreatedAt = now, UpdatedAt = now,
		});
		await db.SaveChangesAsync();
	}

	// ── Rule 1: operator must be checked in ──────────────────────────────────

	[Fact]
	public async Task Open_OperatorNotCheckedIn_ReturnsBadRequest()
	{
		var (db, machine, op1, _, phase) = await TestDbFactory.SeedForWorkSessionTests();
		// No CheckIn event added

		var result = await Controller(db).Open(OpenDto(op1.Id, phase.Id, machine.Id), CancellationToken.None);

		Assert.IsType<BadRequestObjectResult>(result.Result);
	}

	[Fact]
	public async Task Open_OperatorCheckedOut_ReturnsBadRequest()
	{
		var (db, machine, op1, _, phase) = await TestDbFactory.SeedForWorkSessionTests();
		var now = DateTimeOffset.UtcNow;
		db.OperatorShifts.AddRange(
			new OperatorShift { OperatorId = op1.Id, EventType = OperatorEventType.CheckIn,
				EventTime = now.AddHours(-4), Source = "Manual", CreatedAt = now, UpdatedAt = now },
			new OperatorShift { OperatorId = op1.Id, EventType = OperatorEventType.CheckOut,
				EventTime = now.AddHours(-1), Source = "Manual", CreatedAt = now, UpdatedAt = now }
		);
		await db.SaveChangesAsync();

		var result = await Controller(db).Open(OpenDto(op1.Id, phase.Id, machine.Id), CancellationToken.None);

		Assert.IsType<BadRequestObjectResult>(result.Result);
	}

	// ── Rule 2: operator must not be on break ─────────────────────────────────

	[Fact]
	public async Task Open_OperatorOnBreak_ReturnsBadRequest()
	{
		var (db, machine, op1, _, phase) = await TestDbFactory.SeedForWorkSessionTests();
		var now = DateTimeOffset.UtcNow;
		db.OperatorShifts.AddRange(
			new OperatorShift { OperatorId = op1.Id, EventType = OperatorEventType.CheckIn,
				EventTime = now.AddHours(-3), Source = "Manual", CreatedAt = now, UpdatedAt = now },
			new OperatorShift { OperatorId = op1.Id, EventType = OperatorEventType.BreakStart,
				EventTime = now.AddMinutes(-30), Source = "Manual", CreatedAt = now, UpdatedAt = now }
		);
		await db.SaveChangesAsync();

		var result = await Controller(db).Open(OpenDto(op1.Id, phase.Id, machine.Id), CancellationToken.None);

		Assert.IsType<BadRequestObjectResult>(result.Result);
	}

	[Fact]
	public async Task Open_OperatorBreakEnded_Succeeds()
	{
		var (db, machine, op1, _, phase) = await TestDbFactory.SeedForWorkSessionTests();
		var now = DateTimeOffset.UtcNow;
		db.OperatorShifts.AddRange(
			new OperatorShift { OperatorId = op1.Id, EventType = OperatorEventType.CheckIn,
				EventTime = now.AddHours(-3), Source = "Manual", CreatedAt = now, UpdatedAt = now },
			new OperatorShift { OperatorId = op1.Id, EventType = OperatorEventType.BreakStart,
				EventTime = now.AddMinutes(-60), Source = "Manual", CreatedAt = now, UpdatedAt = now },
			new OperatorShift { OperatorId = op1.Id, EventType = OperatorEventType.BreakEnd,
				EventTime = now.AddMinutes(-30), Source = "Manual", CreatedAt = now, UpdatedAt = now }
		);
		await db.SaveChangesAsync();

		var result = await Controller(db).Open(OpenDto(op1.Id, phase.Id, machine.Id), CancellationToken.None);

		Assert.IsType<OkObjectResult>(result.Result);
	}

	// ── Rule 3: machine must not be stopped or in maintenance ─────────────────

	[Fact]
	public async Task Open_MachineStopped_ReturnsBadRequest()
	{
		var (db, machine, op1, _, phase) = await TestDbFactory.SeedForWorkSessionTests();
		await AddCheckIn(db, op1.Id);
		await SetMachineStatus(db, machine.Id, MachineStatus.Stopped);

		var result = await Controller(db).Open(OpenDto(op1.Id, phase.Id, machine.Id), CancellationToken.None);

		Assert.IsType<BadRequestObjectResult>(result.Result);
	}

	[Fact]
	public async Task Open_MachineMaintenance_ReturnsBadRequest()
	{
		var (db, machine, op1, _, phase) = await TestDbFactory.SeedForWorkSessionTests();
		await AddCheckIn(db, op1.Id);
		await SetMachineStatus(db, machine.Id, MachineStatus.Maintenance);

		var result = await Controller(db).Open(OpenDto(op1.Id, phase.Id, machine.Id), CancellationToken.None);

		Assert.IsType<BadRequestObjectResult>(result.Result);
	}

	[Fact]
	public async Task Open_MachineRunning_Succeeds()
	{
		var (db, machine, op1, _, phase) = await TestDbFactory.SeedForWorkSessionTests();
		await AddCheckIn(db, op1.Id);
		await SetMachineStatus(db, machine.Id, MachineStatus.Running);

		var result = await Controller(db).Open(OpenDto(op1.Id, phase.Id, machine.Id), CancellationToken.None);

		Assert.IsType<OkObjectResult>(result.Result);
	}

	// ── Rule 4: machine in Setup allows only Setup sessions ───────────────────

	[Fact]
	public async Task Open_MachineInSetup_WorkSession_ReturnsBadRequest()
	{
		var (db, machine, op1, _, phase) = await TestDbFactory.SeedForWorkSessionTests();
		await AddCheckIn(db, op1.Id);
		await SetMachineStatus(db, machine.Id, MachineStatus.Setup);

		var result = await Controller(db).Open(
			OpenDto(op1.Id, phase.Id, machine.Id, WorkSessionType.Work), CancellationToken.None);

		Assert.IsType<BadRequestObjectResult>(result.Result);
	}

	[Fact]
	public async Task Open_MachineInSetup_SetupSession_Succeeds()
	{
		var (db, machine, op1, _, phase) = await TestDbFactory.SeedForWorkSessionTests();
		await AddCheckIn(db, op1.Id);
		await SetMachineStatus(db, machine.Id, MachineStatus.Setup);

		var result = await Controller(db).Open(
			OpenDto(op1.Id, phase.Id, machine.Id, WorkSessionType.Setup), CancellationToken.None);

		Assert.IsType<OkObjectResult>(result.Result);
	}

	// ── Rule 5: phase must not be Closed or Completed ─────────────────────────

	[Fact]
	public async Task Open_PhaseCompleted_ReturnsBadRequest()
	{
		var (db, machine, op1, _, phase) = await TestDbFactory.SeedForWorkSessionTests();
		await AddCheckIn(db, op1.Id);
		phase.Status = OrderStatus.Completed;
		await db.SaveChangesAsync();

		var result = await Controller(db).Open(OpenDto(op1.Id, phase.Id, machine.Id), CancellationToken.None);

		Assert.IsType<BadRequestObjectResult>(result.Result);
	}

	[Fact]
	public async Task Open_PhaseClosed_ReturnsBadRequest()
	{
		var (db, machine, op1, _, phase) = await TestDbFactory.SeedForWorkSessionTests();
		await AddCheckIn(db, op1.Id);
		phase.Status = OrderStatus.Closed;
		await db.SaveChangesAsync();

		var result = await Controller(db).Open(OpenDto(op1.Id, phase.Id, machine.Id), CancellationToken.None);

		Assert.IsType<BadRequestObjectResult>(result.Result);
	}

	// ── Close edge cases ──────────────────────────────────────────────────────

	[Fact]
	public async Task Close_AlreadyClosedSession_ReturnsBadRequest()
	{
		var (db, machine, op1, _, phase) = await TestDbFactory.SeedForWorkSessionTests();
		var now = DateTimeOffset.UtcNow;
		var session = new WorkSession
		{
			OperatorId = op1.Id, ProductionOrderPhaseId = phase.Id, MachineId = machine.Id,
			SessionType = WorkSessionType.Work, Status = WorkSessionStatus.Closed,
			StartTime = now.AddMinutes(-60), EndTime = now.AddMinutes(-30),
			Source = "Manual", CreatedAt = now, UpdatedAt = now,
		};
		db.WorkSessions.Add(session);
		await db.SaveChangesAsync();

		var result = await Controller(db).Close(session.Id, CancellationToken.None);

		Assert.IsType<BadRequestObjectResult>(result.Result);
	}

	[Fact]
	public async Task Close_NonExistentSession_ReturnsNotFound()
	{
		var (db, _, _, _, _) = await TestDbFactory.SeedForWorkSessionTests();

		var result = await Controller(db).Close(99999, CancellationToken.None);

		Assert.IsType<NotFoundResult>(result.Result);
	}
}
