using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using OpenMES.Data.Common;
using OpenMES.Data.Dtos;
using OpenMES.Data.Entities.Interfaces;
using OpenMES.Data.Interfaces;

namespace OpenMES.Data.Entities;

/// <summary>
/// Represents the explicit placement of a production order phase on a machine.
/// A placement stays open until it is unplaced/closed.
/// </summary>
[Table(nameof(MachinePhasePlacement))]
[PrimaryKey(nameof(Id))]
public class MachinePhasePlacement : IKey<int>, IBaseDates, IDtoAdapter<MachinePhasePlacement, MachinePhasePlacementDto>
{
    public int Id { get; set; }
    public int MachineId { get; set; }
    public int ProductionOrderPhaseId { get; set; }
    public int PlacedByOperatorId { get; set; }
    public DateTimeOffset PlacedAt { get; set; }
    public DateTimeOffset? UnplacedAt { get; set; }
    public MachinePhasePlacementStatus Status { get; set; } = MachinePhasePlacementStatus.Placed;
    [StringLength(20)]
    public string Source { get; set; } = "Manual";
    [StringLength(500)]
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    [ForeignKey(nameof(MachineId))]
    public virtual Machine Machine { get; set; } = null!;

    [ForeignKey(nameof(ProductionOrderPhaseId))]
    public virtual ProductionOrderPhase ProductionOrderPhase { get; set; } = null!;

    [ForeignKey(nameof(PlacedByOperatorId))]
    public virtual Operator PlacedByOperator { get; set; } = null!;

    public static MachinePhasePlacementDto AsDto(MachinePhasePlacement entity) => new()
    {
        Id = entity.Id,
        MachineId = entity.MachineId,
        ProductionOrderPhaseId = entity.ProductionOrderPhaseId,
        PlacedByOperatorId = entity.PlacedByOperatorId,
        PlacedAt = entity.PlacedAt,
        UnplacedAt = entity.UnplacedAt,
        Status = entity.Status,
        Source = entity.Source,
        Notes = entity.Notes,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt,
        MachineCode = entity.Machine?.Code,
        PhaseNumber = entity.ProductionOrderPhase?.PhaseNumber,
        PhaseConfirmNumber = entity.ProductionOrderPhase?.ExternalId,
        PlacedByOperatorName = entity.PlacedByOperator?.Name,
    };

    public static MachinePhasePlacement AsEntity(MachinePhasePlacementDto dto) => new()
    {
        Id = dto.Id,
        MachineId = dto.MachineId,
        ProductionOrderPhaseId = dto.ProductionOrderPhaseId,
        PlacedByOperatorId = dto.PlacedByOperatorId,
        PlacedAt = dto.PlacedAt,
        UnplacedAt = dto.UnplacedAt,
        Status = dto.Status,
        Source = dto.Source,
        Notes = dto.Notes,
        CreatedAt = dto.CreatedAt,
        UpdatedAt = dto.UpdatedAt,
    };
}
