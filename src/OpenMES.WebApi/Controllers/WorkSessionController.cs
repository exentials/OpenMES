using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenMES.Data.Common;
using OpenMES.Data.Contexts;
using OpenMES.Data.Dtos;
using OpenMES.Data.Entities;

namespace OpenMES.WebApi.Controllers;

/// <summary>
/// Manages work sessions — periods of activity declared by operators on production
/// order phases. Exposes open/close endpoints in addition to standard CRUD.
/// Business validation rules (presence check, machine state, etc.) are enforced
/// by the open endpoint before any record is created.
/// </summary>
[Authorize(AuthenticationSchemes = "JWT,TerminalScheme", Roles = "Admin, User, device")]
public class WorkSessionController(OpenMESDbContext dbContext, ILogger<WorkSessionController> logger)
	: RestApiControllerBase<WorkSession, WorkSessionDto, int>(dbContext, logger)
{
	protected override IQueryable<WorkSession> Query => base.Query
		.Include(x => x.Operator)
		.Include(x => x.ProductionOrderPhase)
		.Include(x => x.Machine)
		.OrderByDescending(x => x.StartTime);

	/// <summary>Returns all closed sessions pending ERP export (not yet exported, not reversals).</summary>
	[HttpGet("pending-export")]
	public async Task<ActionResult<IEnumerable<WorkSessionDto>>> GetPendingExport(CancellationToken ct)
	{
		var items = await Query
			.Where(x => x.Status == WorkSessionStatus.Closed
					 && x.ExternalCounterId == null
					 && x.ReversedById == null)
			.OrderBy(x => x.StartTime)
			.Select(x => WorkSession.AsDto(x))
			.ToListAsync(ct);
		return Ok(items);
	}

	/// <summary>Returns all currently open sessions (live shop floor view).</summary>
	[HttpGet("open")]
	public async Task<ActionResult<IEnumerable<WorkSessionDto>>> GetOpen(CancellationToken ct)
	{
		var items = await Query
			.Where(x => x.Status == WorkSessionStatus.Open)
			.OrderBy(x => x.StartTime)
			.Select(x => WorkSession.AsDto(x))
			.ToListAsync(ct);
		return Ok(items);
	}

	/// <summary>Returns all sessions for a specific production order phase.</summary>
	[HttpGet("phase/{phaseId:int}")]
	public async Task<ActionResult<IEnumerable<WorkSessionDto>>> GetByPhase(int phaseId, CancellationToken ct)
	{
		var items = await Query
			.Where(x => x.ProductionOrderPhaseId == phaseId)
			.OrderBy(x => x.StartTime)
			.Select(x => WorkSession.AsDto(x))
			.ToListAsync(ct);
		return Ok(items);
	}

	/// <summary>
	/// Opens a new work session after validating all business rules:
	/// operator presence, machine state, phase status, concurrent session policy.
	/// If AllowConcurrentSessions = false and an open session exists on the same
	/// machine, it is auto-closed before the new one is created.
	/// </summary>
	[HttpPost("open")]
	public async Task<ActionResult<WorkSessionDto>> Open([FromBody] WorkSessionDto dto, CancellationToken ct)
	{
		// 1. Operator must be present (last shift event = CheckIn or BreakEnd)
		var lastShift = await DbContext.OperatorShifts
			.Where(x => x.OperatorId == dto.OperatorId)
			.OrderByDescending(x => x.EventTime)
			.FirstOrDefaultAsync(ct);

		if (lastShift is null ||
			lastShift.EventType == OperatorEventType.CheckOut)
			return BadRequest("Cannot open session: operator is not checked in.");

		if (lastShift.EventType == OperatorEventType.BreakStart)
			return BadRequest("Cannot open session: operator is on break.");

		// 2. Machine must be available
		var machineState = await DbContext.MachineStates
			.Where(x => x.MachineId == dto.MachineId)
			.OrderByDescending(x => x.EventTime)
			.FirstOrDefaultAsync(ct);

		if (machineState is not null)
		{
			if (machineState.Status is MachineStatus.Stopped or MachineStatus.Maintenance)
				return BadRequest($"Cannot open session: machine is {machineState.Status}.");

			if (machineState.Status == MachineStatus.Setup && dto.SessionType == WorkSessionType.Work)
				return BadRequest("Cannot open Work session: machine is in Setup.");
		}

		// 3. Phase must be open
		var phase = await DbContext.ProductionOrderPhases
			.FirstOrDefaultAsync(x => x.Id == dto.ProductionOrderPhaseId, ct);

		if (phase is null)
			return NotFound("Production order phase not found.");

		if (phase.Status == OrderStatus.Completed || phase.Status == OrderStatus.Closed)
			return BadRequest("Cannot open session: phase is already closed.");

		// 4. Concurrent session policy — close ALL open sessions on this machine
		//    regardless of operator (the machine can only serve one session at a time)
		var machine = await DbContext.Machines
			.FirstOrDefaultAsync(x => x.Id == dto.MachineId, ct);

		if (machine is not null && !machine.AllowConcurrentSessions)
		{
			var existing = await Query
				.Where(x => x.MachineId == dto.MachineId
						 && x.Status == WorkSessionStatus.Open)
				.ToListAsync(ct);

			var closeAt = dto.StartTime == default ? DateTimeOffset.UtcNow : dto.StartTime;
			foreach (var s in existing)
			{
				s.Status = WorkSessionStatus.Closed;
				s.EndTime = closeAt;
				s.UpdatedAt = DateTimeOffset.UtcNow;
			}
		}

		// 5. Create the session
		var entity = WorkSession.AsEntity(dto);
		entity.Status = WorkSessionStatus.Open;
		entity.StartTime = dto.StartTime == default ? DateTimeOffset.UtcNow : dto.StartTime;
		entity.CreatedAt = DateTimeOffset.UtcNow;
		entity.UpdatedAt = DateTimeOffset.UtcNow;

		DbContext.WorkSessions.Add(entity);
		await DbContext.SaveChangesAsync(ct);

		var created = await Query
			.Include(x => x.Operator)
			.Include(x => x.ProductionOrderPhase)
			.Include(x => x.Machine)
			.FirstAsync(x => x.Id == entity.Id, ct);

		return Ok(WorkSession.AsDto(created));
	}

	/// <summary>
	/// Closes a work session and recomputes AllocatedMinutes for all closed sessions
	/// on the same phase according to the machine's TimeAllocationMode.
	/// </summary>
	[HttpPost("{id:int}/close")]
	public async Task<ActionResult<WorkSessionDto>> Close(int id, CancellationToken ct)
	{
		var session = await Query
			.Include(x => x.Machine)
			.FirstOrDefaultAsync(x => x.Id == id, ct);

		if (session is null) return NotFound();
		if (session.Status == WorkSessionStatus.Closed) return BadRequest("Session is already closed.");

		session.Status = WorkSessionStatus.Closed;
		session.EndTime = DateTimeOffset.UtcNow;
		session.UpdatedAt = DateTimeOffset.UtcNow;

		await DbContext.SaveChangesAsync(ct);
		await ReallocateMinutes(session.ProductionOrderPhaseId, session.Machine, ct);

		var updated = await Query
			.Include(x => x.Operator)
			.Include(x => x.ProductionOrderPhase)
			.Include(x => x.Machine)
			.FirstAsync(x => x.Id == id, ct);

		return Ok(WorkSession.AsDto(updated));
	}

	// ── Correction / reversal ─────────────────────────────────────────────────

	/// <summary>
	/// Corrects a work session by applying the appropriate strategy based on
	/// whether it has already been exported to the ERP.
	///
	/// Not yet exported (ExternalCounterId = null):
	///   The original session is deleted and a new one is created with the corrected data.
	///
	/// Already exported (ExternalCounterId is set):
	///   1. A reversal session is created with negated AllocatedMinutes and IsReversal = true.
	///   2. The original session is marked with ReversedById pointing to the reversal.
	///   3. A new corrected session is created with the data from the request body.
	///   Both the reversal and the new session will be included in the next ERP export.
	/// </summary>
	[HttpPost("{id:int}/correct")]
	public async Task<ActionResult<WorkSessionDto>> Correct(
		int id, [FromBody] WorkSessionDto corrected, CancellationToken ct)
	{
		var original = await Query.FirstOrDefaultAsync(x => x.Id == id, ct);

		if (original is null) return NotFound();
		if (original.IsReversal) return BadRequest("Cannot correct a reversal record.");
		if (original.ReversedById is not null) return BadRequest("This session has already been reversed.");

		var now = DateTimeOffset.UtcNow;

		if (original.ExternalCounterId is null)
		{
			// Not yet exported — delete and recreate
			DbContext.WorkSessions.Remove(original);

			var replacement = WorkSession.AsEntity(corrected);
			replacement.Id = 0;
			replacement.PhaseExternalId = original.PhaseExternalId;
			replacement.CreatedAt = now;
			replacement.UpdatedAt = now;

			DbContext.WorkSessions.Add(replacement);
			await DbContext.SaveChangesAsync(ct);

			var created = await Query.FirstAsync(x => x.Id == replacement.Id, ct);
			return Ok(WorkSession.AsDto(created));
		}
		else
		{
			// Already exported — create reversal + new corrected record
			var reversal = new WorkSession
			{
				OperatorId = original.OperatorId,
				ProductionOrderPhaseId = original.ProductionOrderPhaseId,
				MachineId = original.MachineId,
				SessionType = original.SessionType,
				Status = WorkSessionStatus.Closed,
				StartTime = original.StartTime,
				EndTime = original.EndTime,
				AllocatedMinutes = -original.AllocatedMinutes,   // negated
				Source = "Reversal",
				PhaseExternalId = original.PhaseExternalId,
				IsReversal = true,
				ReversalOfId = original.Id,
				Notes = $"Reversal of session {original.Id} (ERP: {original.ExternalCounterId})",
				CreatedAt = now,
				UpdatedAt = now,
			};
			DbContext.WorkSessions.Add(reversal);
			await DbContext.SaveChangesAsync(ct);

			// Mark original as reversed
			original.ReversedById = reversal.Id;
			original.UpdatedAt = now;

			// Create new corrected session
			var newSession = WorkSession.AsEntity(corrected);
			newSession.Id = 0;
			newSession.PhaseExternalId = original.PhaseExternalId;
			newSession.IsReversal = false;
			newSession.CreatedAt = now;
			newSession.UpdatedAt = now;

			DbContext.WorkSessions.Add(newSession);
			await DbContext.SaveChangesAsync(ct);

			var created = await Query.FirstAsync(x => x.Id == newSession.Id, ct);
			return Ok(WorkSession.AsDto(created));
		}
	}

	// ── Time allocation ───────────────────────────────────────────────────────

	/// <summary>
	/// Recomputes AllocatedMinutes for all closed sessions on the same
	/// (phase, machine) pair. Sessions on different machines for the same phase
	/// are kept independent — each machine has its own allocation pool.
	/// </summary>
	private async Task ReallocateMinutes(int phaseId, Machine machine, CancellationToken ct)
	{
		// Scope: only sessions on this specific machine for this phase
		var closed = await Query
			.Where(x => x.ProductionOrderPhaseId == phaseId
					 && x.MachineId == machine.Id
					 && x.Status == WorkSessionStatus.Closed)
			.ToListAsync(ct);

		if (closed.Count == 0) return;

		if (machine.TimeAllocationMode == MachineTimeAllocationMode.Uniform)
		{
			var totalMinutes = (decimal)closed.Sum(x => (x.EndTime!.Value - x.StartTime).TotalMinutes);
			var sessionsByOperator = closed.GroupBy(x => x.OperatorId).ToList();
			var perOperatorShare = sessionsByOperator.Count > 0 ? totalMinutes / sessionsByOperator.Count : 0;

			foreach (var group in sessionsByOperator)
			{
				var operatorTotalMinutes = (decimal)group.Sum(x => (x.EndTime!.Value - x.StartTime).TotalMinutes);
				foreach (var s in group)
				{
					var sessionMinutes = (decimal)(s.EndTime!.Value - s.StartTime).TotalMinutes;
					var proportion = operatorTotalMinutes > 0 ? sessionMinutes / operatorTotalMinutes : 0;
					s.AllocatedMinutes = Math.Round(perOperatorShare * proportion, 3);
					s.UpdatedAt = DateTimeOffset.UtcNow;
				}
			}
		}
		else // Proportional
		{
			// Proportional: each session is allocated its own raw duration.
			foreach (var s in closed)
			{
				var rawMinutes = (decimal)(s.EndTime!.Value - s.StartTime).TotalMinutes;
				s.AllocatedMinutes = Math.Round(rawMinutes, 3);
				s.UpdatedAt = DateTimeOffset.UtcNow;
			}
		}

		await DbContext.SaveChangesAsync(ct);
	}
}
