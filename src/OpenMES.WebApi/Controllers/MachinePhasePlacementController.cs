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

        var latestShift = await DbContext.OperatorShifts
            .Where(x => x.OperatorId == dto.PlacedByOperatorId)
            .OrderByDescending(x => x.EventTime)
            .FirstOrDefaultAsync(ct);

        if (latestShift is null || latestShift.EventType == OperatorEventType.CheckOut)
            return BadRequest("Cannot place phase: operator is not checked in.");

        if (latestShift.EventType == OperatorEventType.BreakStart)
            return BadRequest("Cannot place phase: operator is on break.");

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

    /// <summary>
    /// Unplaces (closes) an open placement record.
    /// </summary>
    [HttpPost("{id:int}/unplace")]
    public async Task<ActionResult<MachinePhasePlacementDto>> Unplace(int id, CancellationToken ct)
    {
        var placement = await Query.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (placement is null)
            return NotFound();

        if (placement.UnplacedAt is not null || placement.Status == MachinePhasePlacementStatus.Closed)
            return BadRequest("Placement is already closed.");

        placement.UnplacedAt = DateTimeOffset.UtcNow;
        placement.Status = MachinePhasePlacementStatus.Closed;
        placement.UpdatedAt = DateTimeOffset.UtcNow;

        await DbContext.SaveChangesAsync(ct);

        var updated = await Query.FirstAsync(x => x.Id == id, ct);
        return Ok(MachinePhasePlacement.AsDto(updated));
    }

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
}
