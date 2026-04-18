using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenMES.Data.Common;
using OpenMES.Data.Contexts;
using OpenMES.Data.Dtos;
using OpenMES.Data.Entities;

namespace OpenMES.WebApi.Controllers;

/// <summary>
/// Manages operator presence events (CheckIn, CheckOut, BreakStart, BreakEnd).
/// Records are append-only. The current state of an operator is derived from
/// the most recent event via the /current endpoint.
/// On CheckOut, all open WorkSessions for the operator are force-closed.
/// </summary>
[Authorize(AuthenticationSchemes = "JWT,TerminalScheme", Roles = "Admin, User, device")]
public class OperatorShiftController(OpenMESDbContext dbContext, ILogger<OperatorShiftController> logger)
	: RestApiControllerBase<OperatorShift, OperatorShiftDto, int>(dbContext, logger)
{
	protected override IQueryable<OperatorShift> Query => base.Query
		.Include(x => x.Operator);

	/// <summary>
	/// Records a presence event. On CheckOut, all open WorkSessions for this
	/// operator are force-closed with EndTime = CheckOut.EventTime.
	/// Validates state transitions before saving.
	/// </summary>
	protected override async Task<int> CreateAsync(
		OperatorShiftDto dto, CancellationToken cancellationToken = default)
	{
		// Validate state transitions
		var lastEvent = await DbContext.OperatorShifts
			.Where(x => x.OperatorId == dto.OperatorId)
			.OrderByDescending(x => x.EventTime)
			.FirstOrDefaultAsync(cancellationToken);

		var lastType = lastEvent?.EventType;

		var validationError = (dto.EventType, lastType) switch
		{
			(OperatorEventType.CheckIn, OperatorEventType.CheckIn) => "Operator already has an active shift.",
			(OperatorEventType.CheckIn, OperatorEventType.BreakStart) => "Cannot check in while on break. End the break first.",
			(OperatorEventType.CheckOut, null) => "No active shift to check out from.",
			(OperatorEventType.CheckOut, OperatorEventType.CheckOut) => "No active shift to check out from.",
			(OperatorEventType.BreakStart, null) => "Operator is not checked in.",
			(OperatorEventType.BreakStart, OperatorEventType.CheckOut) => "Operator is not checked in.",
			(OperatorEventType.BreakStart, OperatorEventType.BreakStart) => "Operator is already on break.",
			(OperatorEventType.BreakEnd, null) => "Operator is not on break.",
			(OperatorEventType.BreakEnd, OperatorEventType.CheckIn) => "Operator is not on break.",
			(OperatorEventType.BreakEnd, OperatorEventType.BreakEnd) => "Operator is not on break.",
			(OperatorEventType.BreakEnd, OperatorEventType.CheckOut) => "Operator is not on break.",
			_ => null
		};

		if (validationError is not null)
			throw new ProblemException("Invalid shift event", validationError);

		// On CheckOut, force-close all open WorkSessions
		if (dto.EventType == OperatorEventType.CheckOut)
		{
			var checkOutTime = dto.EventTime == default ? DateTimeOffset.UtcNow : dto.EventTime;
			var openSessions = await DbContext.WorkSessions
				.Include(x => x.Machine)
				.Where(x => x.OperatorId == dto.OperatorId && x.Status == WorkSessionStatus.Open)
				.ToListAsync(cancellationToken);

			foreach (var session in openSessions)
			{
				session.Status = WorkSessionStatus.Closed;
				session.EndTime = checkOutTime;
				session.UpdatedAt = DateTimeOffset.UtcNow;
			}

			if (openSessions.Count > 0)
				await ReallocateMinutesForOperator(openSessions, cancellationToken);
		}

		// Save the shift event
		var entity = OperatorShift.AsEntity(dto);
		entity.EventTime = dto.EventTime == default ? DateTimeOffset.UtcNow : dto.EventTime;
		entity.CreatedAt = DateTimeOffset.UtcNow;
		entity.UpdatedAt = DateTimeOffset.UtcNow;

		DbContext.OperatorShifts.Add(entity);
		await DbContext.SaveChangesAsync(cancellationToken);
		return entity.Id;
	}

	/// <summary>
	/// Returns the current presence status of an operator derived from their
	/// most recent shift event today.
	/// </summary>
	[HttpGet("operator/{operatorId:int}/current")]
	public async Task<ActionResult<OperatorShiftDto?>> GetCurrentStatus(
		int operatorId, CancellationToken cancellationToken)
	{
		var latest = await Query
			.Where(x => x.OperatorId == operatorId)
			.OrderByDescending(x => x.EventTime)
			.FirstOrDefaultAsync(cancellationToken);

		return Ok(latest is null ? null : OperatorShift.AsDto(latest));
	}

	/// <summary>Returns all shift events for an operator on a specific date (yyyy-MM-dd).</summary>
	[HttpGet("operator/{operatorId:int}/date/{date}")]
	public async Task<ActionResult<IEnumerable<OperatorShiftDto>>> GetByDate(
		int operatorId, string date, CancellationToken cancellationToken)
	{
		if (!DateOnly.TryParse(date, out var parsedDate))
			return BadRequest("Invalid date format. Use yyyy-MM-dd.");

		var start = new DateTimeOffset(parsedDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
		var end = start.AddDays(1);

		var items = await Query
			.Where(x => x.OperatorId == operatorId && x.EventTime >= start && x.EventTime < end)
			.OrderBy(x => x.EventTime)
			.Select(x => OperatorShift.AsDto(x))
			.ToListAsync(cancellationToken);

		return Ok(items);
	}

	/// <summary>
	/// Returns operators currently present on site (latest shift event is CheckIn or BreakEnd).
	/// Optional plantId filters operators by plant.
	/// </summary>
	[HttpGet("present")]
	public async Task<ActionResult<IEnumerable<OperatorDto>>> GetPresent(
		[FromQuery] int? plantId,
		CancellationToken cancellationToken)
	{
		var operatorsQuery = DbContext.Operators.AsQueryable();
		if (plantId.HasValue)
		{
			operatorsQuery = operatorsQuery.Where(x => x.PlantId == plantId.Value);
		}

		var operators = await operatorsQuery
			.OrderBy(x => x.Name)
			.ToListAsync(cancellationToken);

		if (operators.Count == 0)
		{
			return Ok(Array.Empty<OperatorDto>());
		}

		var operatorIds = operators.Select(x => x.Id).ToHashSet();

		var latestEvents = await DbContext.OperatorShifts
			.Where(x => operatorIds.Contains(x.OperatorId))
			.OrderByDescending(x => x.EventTime)
			.ToListAsync(cancellationToken);

		var presentIds = latestEvents
			.GroupBy(x => x.OperatorId)
			.Select(g => g.First())
			.Where(x => x.EventType is OperatorEventType.CheckIn or OperatorEventType.BreakEnd)
			.Select(x => x.OperatorId)
			.ToHashSet();

		var present = operators
			.Where(x => presentIds.Contains(x.Id))
			.Select(Operator.AsDto)
			.ToList();

		return Ok(present);
	}

	// ── Helpers ───────────────────────────────────────────────────────────────

	private async Task ReallocateMinutesForOperator(
		IEnumerable<WorkSession> forcedSessions, CancellationToken ct)
	{
		// Group by (phase, machine) — same scope as WorkSessionController.ReallocateMinutes
		var groups = forcedSessions
			.GroupBy(x => (x.ProductionOrderPhaseId, x.MachineId));

		foreach (var group in groups)
		{
			var (phaseId, machineId) = group.Key;
			var machine = group.First().Machine;
			if (machine is null) continue;

			var allClosed = await DbContext.WorkSessions
				.Where(x => x.ProductionOrderPhaseId == phaseId
						 && x.MachineId == machineId
						 && x.Status == WorkSessionStatus.Closed)
				.ToListAsync(ct);

			if (allClosed.Count == 0) continue;

			if (machine.TimeAllocationMode == MachineTimeAllocationMode.Uniform)
			{
				var total = (decimal)allClosed.Sum(x => (x.EndTime!.Value - x.StartTime).TotalMinutes);
				var distinct = allClosed.Select(x => x.OperatorId).Distinct().Count();
				var perOp = distinct > 0 ? total / distinct : 0;
				foreach (var s in allClosed) { s.AllocatedMinutes = perOp; s.UpdatedAt = DateTimeOffset.UtcNow; }
			}
			else
			{
				foreach (var s in allClosed)
				{
					var raw = (decimal)(s.EndTime!.Value - s.StartTime).TotalMinutes;
					s.AllocatedMinutes = Math.Round(raw, 3);
					s.UpdatedAt = DateTimeOffset.UtcNow;
				}
			}
		}

		await DbContext.SaveChangesAsync(ct);
	}
}
