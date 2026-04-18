using OpenMES.Data.Common;
using OpenMES.Data.Dtos.Resources;
using System.ComponentModel.DataAnnotations;

namespace OpenMES.Data.Dtos;

public class MachineStopDto : IKey<int>
{
	/// <summary>Unique identifier of the machine stop event.</summary>
	public int Id { get; set; }

	/// <summary>FK to the machine that experienced the stop.</summary>
	[Display(Name = nameof(DtoResources.MachineStop_MachineId), ResourceType = typeof(DtoResources))]
	public int MachineId { get; set; }

	/// <summary>FK to the production order phase active at the time of the stop. Null if not linked to a phase.</summary>
	[Display(Name = nameof(DtoResources.MachineStop_ProductionOrderPhaseId), ResourceType = typeof(DtoResources))]
	public int? ProductionOrderPhaseId { get; set; }

	/// <summary>FK to the configured reason code for this stop.</summary>
	[Display(Name = nameof(DtoResources.MachineStop_MachineStopReasonId), ResourceType = typeof(DtoResources))]
	public int MachineStopReasonId { get; set; }

	/// <summary>Date and time when the machine stopped.</summary>
	[Display(Name = nameof(DtoResources.MachineStop_StartDate), ResourceType = typeof(DtoResources))]
	public DateTimeOffset StartDate { get; set; }

	/// <summary>Date and time when the machine resumed. Null if the stop is still ongoing.</summary>
	[Display(Name = nameof(DtoResources.MachineStop_EndDate), ResourceType = typeof(DtoResources))]
	public DateTimeOffset? EndDate { get; set; }

	/// <summary>Free-text notes about the stop event.</summary>
	[StringLength(500)]
	[Display(Name = nameof(DtoResources.MachineStop_Notes), ResourceType = typeof(DtoResources))]
	public string? Notes { get; set; }

	/// <summary>Timestamp of record creation (UTC).</summary>
	public DateTimeOffset CreatedAt { get; set; }

	/// <summary>Timestamp of last record update (UTC).</summary>
	public DateTimeOffset UpdatedAt { get; set; }

	// Denormalized for display
	/// <summary>Code of the machine that stopped (denormalized for display).</summary>
	[Display(Name = nameof(DtoResources.MachineStop_MachineCode), ResourceType = typeof(DtoResources))]
	public string? MachineCode { get; set; }

	/// <summary>Code of the stop reason (denormalized for display).</summary>
	[Display(Name = nameof(DtoResources.MachineStop_ReasonCode), ResourceType = typeof(DtoResources))]
	public string? MachineStopReasonCode { get; set; }

	/// <summary>Description of the stop reason (denormalized for display).</summary>
	[Display(Name = nameof(DtoResources.MachineStop_ReasonDescription), ResourceType = typeof(DtoResources))]
	public string? MachineStopReasonDescription { get; set; }

	/// <summary>Category of the stop reason (denormalized for display).</summary>
	[Display(Name = nameof(DtoResources.MachineStop_ReasonCategory), ResourceType = typeof(DtoResources))]
	public MachineStopCategory? MachineStopReasonCategory { get; set; }

	public override string ToString() => $"{MachineCode} - {StartDate:g}";
}
