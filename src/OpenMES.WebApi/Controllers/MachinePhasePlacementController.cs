using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenMES.Data.Common;
using OpenMES.Data.Contexts;
using OpenMES.Data.Dtos;
using OpenMES.Data.Entities;

namespace OpenMES.WebApi.Controllers;

/// <summary>
/// Manages explicit placement/unplacement of production phases on machines.
/// Also orchestrates setup/work lifecycle transitions for placed phases.
/// </summary>
[Authorize(AuthenticationSchemes = "JWT,TerminalScheme", Roles = "Admin, User, device")]
public class MachinePhasePlacementController(OpenMESDbContext dbContext, ILogger<MachinePhasePlacementController> logger)
    : RestApiControllerBase<MachinePhasePlacement, MachinePhasePlacementDto, int>(dbContext, logger)
{
    protected override IQueryable<MachinePhasePlacement> Query => base.Query
        .Include(x => x.Machine)
        .Include(x => x.ProductionOrderPhase)
        .Include(x => x.PlacedByOperator)
        .OrderByDescending(x => x.PlacedAt);

    /// <summary>
    /// Places a production order phase on a machine after validating coherence
    /// (operator presence, machine/phase existence, work center compatibility,
    /// and duplicate open placement prevention).
    /// </summary>
    [HttpPost("place")]
    public async Task<ActionResult<MachinePhasePlacementDto>> Place([FromBody] MachinePhasePlacementDto dto, CancellationToken ct)
    {
        var machine = await DbContext.Machines
            .FirstOrDefaultAsync(x => x.Id == dto.MachineId, ct);
        if (machine is null)
            return NotFound("Machine not found.");

        var phase = await DbContext.ProductionOrderPhases
            .FirstOrDefaultAsync(x => x.Id == dto.ProductionOrderPhaseId, ct);
        if (phase is null)
            return NotFound("Production order phase not found.");

        if (phase.Status is OrderStatus.Closed or OrderStatus.Completed)
            return BadRequest("Cannot place phase: phase is already closed.");

        if (phase.WorkCenterId != machine.WorkCenterId)
            return BadRequest("Cannot place phase: machine and phase belong to different work centers.");

        var presenceError = await ValidateOperatorPresenceAsync(dto.PlacedByOperatorId, ct);
        if (presenceError is not null)
            return BadRequest(presenceError);

        var existing = await Query
            .FirstOrDefaultAsync(x =>
                x.MachineId == dto.MachineId &&
                x.ProductionOrderPhaseId == dto.ProductionOrderPhaseId &&
                x.UnplacedAt == null, ct);

        if (existing is not null)
            return BadRequest("Phase is already placed on this machine.");

        var entity = MachinePhasePlacement.AsEntity(dto);
        entity.PlacedAt = dto.PlacedAt == default ? DateTimeOffset.UtcNow : dto.PlacedAt;
        entity.UnplacedAt = null;
        entity.Status = MachinePhasePlacementStatus.Placed;
        entity.Source = string.IsNullOrWhiteSpace(dto.Source) ? "Terminal" : dto.Source;
        entity.CreatedAt = DateTimeOffset.UtcNow;
        entity.UpdatedAt = DateTimeOffset.UtcNow;

        DbContext.MachinePhasePlacements.Add(entity);
        await DbContext.SaveChangesAsync(ct);

        var created = await Query.FirstAsync(x => x.Id == entity.Id, ct);
        return Ok(MachinePhasePlacement.AsDto(created));
    }

    [HttpPost("{id:int}/start-setup")]
    public async Task<ActionResult<MachinePhasePlacementDto>> StartSetup(int id, [FromQuery] int? operatorId, CancellationToken ct)
    {
        var placement = await Query.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (placement is null) return NotFound();
        if (placement.UnplacedAt is not null || placement.Status == MachinePhasePlacementStatus.Closed)
            return BadRequest("Cannot start setup: placement is closed.");
        if (placement.Status != MachinePhasePlacementStatus.Placed)
            return BadRequest($"Cannot start setup from status {placement.Status}.");

        var resolvedOperatorId = operatorId ?? placement.PlacedByOperatorId;
        var presenceError = await ValidateOperatorPresenceAsync(resolvedOperatorId, ct);
        if (presenceError is not null) return BadRequest(presenceError);

        var openError = await OpenSessionForPlacementAsync(placement, WorkSessionType.Setup, resolvedOperatorId, ct);
        if (openError is not null) return BadRequest(openError);

        placement.PlacedByOperatorId = resolvedOperatorId;
        placement.Status = MachinePhasePlacementStatus.InSetup;
        placement.UpdatedAt = DateTimeOffset.UtcNow;

        await DbContext.SaveChangesAsync(ct);
        var updated = await Query.FirstAsync(x => x.Id == id, ct);
        return Ok(MachinePhasePlacement.AsDto(updated));
    }

    [HttpPost("{id:int}/pause-setup")]
    public async Task<ActionResult<MachinePhasePlacementDto>> PauseSetup(int id, CancellationToken ct)
    {
        var placement = await Query.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (placement is null) return NotFound();
        if (placement.Status != MachinePhasePlacementStatus.InSetup)
            return BadRequest($"Cannot pause setup from status {placement.Status}.");

        var closed = await CloseOpenSessionsAsync(placement, WorkSessionType.Setup, ct);
        if (closed == 0) return BadRequest("Cannot pause setup: no active setup session found.");

        placement.Status = MachinePhasePlacementStatus.SetupPaused;
        placement.UpdatedAt = DateTimeOffset.UtcNow;
        await DbContext.SaveChangesAsync(ct);

        var updated = await Query.FirstAsync(x => x.Id == id, ct);
        return Ok(MachinePhasePlacement.AsDto(updated));
    }

    [HttpPost("{id:int}/resume-setup")]
    public async Task<ActionResult<MachinePhasePlacementDto>> ResumeSetup(int id, [FromQuery] int? operatorId, CancellationToken ct)
    {
        var placement = await Query.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (placement is null) return NotFound();
        if (placement.Status != MachinePhasePlacementStatus.SetupPaused)
            return BadRequest($"Cannot resume setup from status {placement.Status}.");

        var resolvedOperatorId = operatorId ?? placement.PlacedByOperatorId;
        var presenceError = await ValidateOperatorPresenceAsync(resolvedOperatorId, ct);
        if (presenceError is not null) return BadRequest(presenceError);

        var openError = await OpenSessionForPlacementAsync(placement, WorkSessionType.Setup, resolvedOperatorId, ct);
        if (openError is not null) return BadRequest(openError);

        placement.PlacedByOperatorId = resolvedOperatorId;
        placement.Status = MachinePhasePlacementStatus.InSetup;
        placement.UpdatedAt = DateTimeOffset.UtcNow;

        await DbContext.SaveChangesAsync(ct);
        var updated = await Query.FirstAsync(x => x.Id == id, ct);
        return Ok(MachinePhasePlacement.AsDto(updated));
    }

    [HttpPost("{id:int}/start-work")]
    public async Task<ActionResult<MachinePhasePlacementDto>> StartWork(int id, [FromQuery] int? operatorId, CancellationToken ct)
    {
        var placement = await Query.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (placement is null) return NotFound();
        if (placement.UnplacedAt is not null || placement.Status == MachinePhasePlacementStatus.Closed)
            return BadRequest("Cannot start work: placement is closed.");

        if (placement.Status is not (MachinePhasePlacementStatus.Placed or MachinePhasePlacementStatus.InSetup or MachinePhasePlacementStatus.SetupPaused))
            return BadRequest($"Cannot start work from status {placement.Status}.");

        var resolvedOperatorId = operatorId ?? placement.PlacedByOperatorId;
        var presenceError = await ValidateOperatorPresenceAsync(resolvedOperatorId, ct);
        if (presenceError is not null) return BadRequest(presenceError);

        if (placement.Status is MachinePhasePlacementStatus.InSetup or MachinePhasePlacementStatus.SetupPaused)
        {
            await CloseOpenSessionsAsync(placement, WorkSessionType.Setup, ct);
        }

        var openError = await OpenSessionForPlacementAsync(placement, WorkSessionType.Work, resolvedOperatorId, ct);
        if (openError is not null) return BadRequest(openError);

        placement.PlacedByOperatorId = resolvedOperatorId;
        placement.Status = MachinePhasePlacementStatus.InWork;
        placement.UpdatedAt = DateTimeOffset.UtcNow;

        await DbContext.SaveChangesAsync(ct);
        var updated = await Query.FirstAsync(x => x.Id == id, ct);
        return Ok(MachinePhasePlacement.AsDto(updated));
    }

    [HttpPost("{id:int}/pause-work")]
    public async Task<ActionResult<MachinePhasePlacementDto>> PauseWork(int id, CancellationToken ct)
    {
        var placement = await Query.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (placement is null) return NotFound();
        if (placement.Status != MachinePhasePlacementStatus.InWork)
            return BadRequest($"Cannot pause work from status {placement.Status}.");

        var closed = await CloseOpenSessionsAsync(placement, WorkSessionType.Work, ct);
        if (closed == 0) return BadRequest("Cannot pause work: no active work session found.");

        placement.Status = MachinePhasePlacementStatus.WorkPaused;
        placement.UpdatedAt = DateTimeOffset.UtcNow;
        await DbContext.SaveChangesAsync(ct);

        var updated = await Query.FirstAsync(x => x.Id == id, ct);
        return Ok(MachinePhasePlacement.AsDto(updated));
    }

    [HttpPost("{id:int}/resume-work")]
    public async Task<ActionResult<MachinePhasePlacementDto>> ResumeWork(int id, [FromQuery] int? operatorId, CancellationToken ct)
    {
        var placement = await Query.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (placement is null) return NotFound();
        if (placement.Status != MachinePhasePlacementStatus.WorkPaused)
            return BadRequest($"Cannot resume work from status {placement.Status}.");

        var resolvedOperatorId = operatorId ?? placement.PlacedByOperatorId;
        var presenceError = await ValidateOperatorPresenceAsync(resolvedOperatorId, ct);
        if (presenceError is not null) return BadRequest(presenceError);

        var openError = await OpenSessionForPlacementAsync(placement, WorkSessionType.Work, resolvedOperatorId, ct);
        if (openError is not null) return BadRequest(openError);

        placement.PlacedByOperatorId = resolvedOperatorId;
        placement.Status = MachinePhasePlacementStatus.InWork;
        placement.UpdatedAt = DateTimeOffset.UtcNow;

        await DbContext.SaveChangesAsync(ct);
        var updated = await Query.FirstAsync(x => x.Id == id, ct);
        return Ok(MachinePhasePlacement.AsDto(updated));
    }

    /// <summary>
    /// Closes a placement and its associated active sessions.
    /// </summary>
    [HttpPost("{id:int}/close")]
    public async Task<ActionResult<MachinePhasePlacementDto>> Close(int id, CancellationToken ct)
    {
        var placement = await Query.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (placement is null) return NotFound();

        if (placement.UnplacedAt is not null || placement.Status == MachinePhasePlacementStatus.Closed)
            return BadRequest("Placement is already closed.");

        var hasOpenSessions = await DbContext.WorkSessions
            .AnyAsync(x =>
                x.MachineId == placement.MachineId &&
                x.ProductionOrderPhaseId == placement.ProductionOrderPhaseId &&
                x.Status == WorkSessionStatus.Open, ct);

        if (hasOpenSessions)
            return BadRequest("Cannot close placement: there are active work sessions for this phase on this machine.");

        placement.UnplacedAt = DateTimeOffset.UtcNow;
        placement.Status = MachinePhasePlacementStatus.Closed;
        placement.UpdatedAt = DateTimeOffset.UtcNow;

        await DbContext.SaveChangesAsync(ct);

        var updated = await Query.FirstAsync(x => x.Id == id, ct);
        return Ok(MachinePhasePlacement.AsDto(updated));
    }

    [HttpPost("{id:int}/unplace")]
    public Task<ActionResult<MachinePhasePlacementDto>> Unplace(int id, CancellationToken ct)
        => Close(id, ct);

    /// <summary>
    /// Returns all currently open placements for the given machine.
    /// </summary>
    [HttpGet("machine/{machineId:int}/open")]
    public async Task<ActionResult<IEnumerable<MachinePhasePlacementDto>>> GetOpenByMachine(int machineId, CancellationToken ct)
    {
        var items = await Query
            .Where(x => x.MachineId == machineId && x.UnplacedAt == null)
            .OrderBy(x => x.PlacedAt)
            .Select(x => MachinePhasePlacement.AsDto(x))
            .ToListAsync(ct);

        return Ok(items);
    }

    private async Task<string?> ValidateOperatorPresenceAsync(int operatorId, CancellationToken ct)
    {
        var latestShift = await DbContext.OperatorShifts
            .Where(x => x.OperatorId == operatorId)
            .OrderByDescending(x => x.EventTime)
            .FirstOrDefaultAsync(ct);

        if (latestShift is null || latestShift.EventType == OperatorEventType.CheckOut)
            return "Operator is not checked in.";

        if (latestShift.EventType == OperatorEventType.BreakStart)
            return "Operator is on break.";

        return null;
    }

    private async Task<string?> OpenSessionForPlacementAsync(
        MachinePhasePlacement placement,
        WorkSessionType type,
        int operatorId,
        CancellationToken ct)
    {
        if (placement.ProductionOrderPhase.Status is OrderStatus.Closed or OrderStatus.Completed)
            return "Cannot open activity: phase is already closed.";

        var machineState = await DbContext.MachineStates
            .Where(x => x.MachineId == placement.MachineId)
            .OrderByDescending(x => x.EventTime)
            .FirstOrDefaultAsync(ct);

        if (machineState is not null)
        {
            if (machineState.Status is MachineStatus.Stopped or MachineStatus.Maintenance)
                return $"Cannot open activity: machine is {machineState.Status}.";

            if (machineState.Status == MachineStatus.Setup && type == WorkSessionType.Work)
                return "Cannot open Work activity: machine is in Setup state.";
        }

        var openOnPlacement = await DbContext.WorkSessions
            .Where(x => x.MachineId == placement.MachineId
                     && x.ProductionOrderPhaseId == placement.ProductionOrderPhaseId
                     && x.Status == WorkSessionStatus.Open)
            .ToListAsync(ct);

        if (openOnPlacement.Any(x => x.SessionType == type))
            return $"Cannot open activity: an open {type} session already exists for this placement.";

        if (openOnPlacement.Any(x => x.SessionType != type))
            return "Cannot open activity: another session type is already active for this placement.";

        if (!placement.Machine.AllowConcurrentSessions)
        {
            var existingMachineSessions = await DbContext.WorkSessions
                .Where(x => x.MachineId == placement.MachineId && x.Status == WorkSessionStatus.Open)
                .ToListAsync(ct);

            var closeAt = DateTimeOffset.UtcNow;
            foreach (var s in existingMachineSessions)
            {
                s.Status = WorkSessionStatus.Closed;
                s.EndTime = closeAt;
                s.UpdatedAt = DateTimeOffset.UtcNow;
            }
        }

        var now = DateTimeOffset.UtcNow;
        DbContext.WorkSessions.Add(new WorkSession
        {
            OperatorId = operatorId,
            MachineId = placement.MachineId,
            ProductionOrderPhaseId = placement.ProductionOrderPhaseId,
            SessionType = type,
            Status = WorkSessionStatus.Open,
            StartTime = now,
            Source = "Terminal",
            CreatedAt = now,
            UpdatedAt = now,
        });

        return null;
    }

    private async Task<int> CloseOpenSessionsAsync(MachinePhasePlacement placement, WorkSessionType type, CancellationToken ct)
    {
        var sessions = await DbContext.WorkSessions
            .Where(x => x.MachineId == placement.MachineId
                     && x.ProductionOrderPhaseId == placement.ProductionOrderPhaseId
                     && x.SessionType == type
                     && x.Status == WorkSessionStatus.Open)
            .ToListAsync(ct);

        if (sessions.Count == 0)
            return 0;

        var now = DateTimeOffset.UtcNow;
        foreach (var s in sessions)
        {
            s.Status = WorkSessionStatus.Closed;
            s.EndTime = now;
            s.UpdatedAt = now;
        }

        return sessions.Count;
    }
}
