using System.ComponentModel.DataAnnotations;
using OpenMES.Data.Common;
using OpenMES.Data.Dtos.Resources;

namespace OpenMES.Data.Dtos;

public class MachinePhasePlacementDto : IKey<int>
{
    /// <summary>Unique identifier of the placement record.</summary>
    public int Id { get; set; }

    /// <summary>FK to the machine where the phase is placed.</summary>
    [Display(Name = nameof(DtoResources.MachinePhasePlacement_MachineId), ResourceType = typeof(DtoResources))]
    public int MachineId { get; set; }

    /// <summary>FK to the production order phase currently placed on the machine.</summary>
    [Display(Name = nameof(DtoResources.MachinePhasePlacement_ProductionOrderPhaseId), ResourceType = typeof(DtoResources))]
    public int ProductionOrderPhaseId { get; set; }

    /// <summary>FK to the operator who placed the phase.</summary>
    [Display(Name = nameof(DtoResources.MachinePhasePlacement_PlacedByOperatorId), ResourceType = typeof(DtoResources))]
    public int PlacedByOperatorId { get; set; }

    /// <summary>Date/time when the phase was placed on the machine.</summary>
    [Display(Name = nameof(DtoResources.MachinePhasePlacement_PlacedAt), ResourceType = typeof(DtoResources))]
    public DateTimeOffset PlacedAt { get; set; }

    /// <summary>Date/time when the phase was unplaced from the machine. Null while placement is open.</summary>
    [Display(Name = nameof(DtoResources.MachinePhasePlacement_UnplacedAt), ResourceType = typeof(DtoResources))]
    public DateTimeOffset? UnplacedAt { get; set; }

    /// <summary>Lifecycle status of the placement.</summary>
    [Display(Name = nameof(DtoResources.MachinePhasePlacement_Status), ResourceType = typeof(DtoResources))]
    public MachinePhasePlacementStatus Status { get; set; } = MachinePhasePlacementStatus.Placed;

    /// <summary>Origin of the placement action (Manual/Terminal/Machine).</summary>
    [StringLength(20)]
    [Display(Name = nameof(DtoResources.MachinePhasePlacement_Source), ResourceType = typeof(DtoResources))]
    public string Source { get; set; } = "Manual";

    /// <summary>Optional notes for this placement.</summary>
    [StringLength(500)]
    [Display(Name = nameof(DtoResources.MachinePhasePlacement_Notes), ResourceType = typeof(DtoResources))]
    public string? Notes { get; set; }

    /// <summary>Timestamp of record creation (UTC).</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Timestamp of last record update (UTC).</summary>
    public DateTimeOffset UpdatedAt { get; set; }

    // Denormalized for display
    /// <summary>Machine code (denormalized for display).</summary>
    [Display(Name = nameof(DtoResources.MachinePhasePlacement_MachineCode), ResourceType = typeof(DtoResources))]
    public string? MachineCode { get; set; }

    /// <summary>Phase number (denormalized for display).</summary>
    [Display(Name = nameof(DtoResources.MachinePhasePlacement_PhaseNumber), ResourceType = typeof(DtoResources))]
    public string? PhaseNumber { get; set; }

    /// <summary>Phase confirmation number (denormalized for display).</summary>
    [Display(Name = nameof(DtoResources.MachinePhasePlacement_PhaseConfirmNumber), ResourceType = typeof(DtoResources))]
    public string? PhaseConfirmNumber { get; set; }

    /// <summary>Operator name who placed the phase (denormalized for display).</summary>
    [Display(Name = nameof(DtoResources.MachinePhasePlacement_PlacedByOperatorName), ResourceType = typeof(DtoResources))]
    public string? PlacedByOperatorName { get; set; }
}
