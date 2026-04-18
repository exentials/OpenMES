using OpenMES.Data.Common;
using OpenMES.Data.Dtos.Resources;
using System.ComponentModel.DataAnnotations;

namespace OpenMES.Data.Dtos;

public class ProductionJobDto : IKey<int>
{
	/// <summary>Unique identifier of the production job.</summary>
	public int Id { get; set; }

	/// <summary>FK to the production order phase this job is linked to.</summary>
	[Required]
	[Display(Name = nameof(DtoResources.ProductionJob_ProductionOrderPhaseId), ResourceType = typeof(DtoResources))]
	public int ProductionOrderPhaseId { get; set; }

	/// <summary>Machine program identifier (NC program name, nesting program, etc.).</summary>
	[Required, StringLength(30)]
	[Display(Name = nameof(DtoResources.ProductionJob_JobId), ResourceType = typeof(DtoResources))]
	public string JobId { get; set; } = null!;

	/// <summary>Planned number of pieces this job is expected to produce.</summary>
	[Display(Name = nameof(DtoResources.ProductionJob_PlannedQuantity), ResourceType = typeof(DtoResources))]
	public decimal PlannedQuantity { get; set; }

	/// <summary>Planned setup duration in minutes.</summary>
	[Display(Name = nameof(DtoResources.ProductionJob_PlannedSetupTime), ResourceType = typeof(DtoResources))]
	public decimal PlannedSetupTime { get; set; }

	/// <summary>Planned run duration in minutes.</summary>
	[Display(Name = nameof(DtoResources.ProductionJob_PlannedRunTime), ResourceType = typeof(DtoResources))]
	public decimal PlannedRunTime { get; set; }

	/// <summary>Timestamp of record creation (UTC).</summary>
	public DateTimeOffset CreatedAt { get; set; }

	/// <summary>Timestamp of last record update (UTC).</summary>
	public DateTimeOffset UpdatedAt { get; set; }

	// Denormalized for display
	/// <summary>Phase sequence number (denormalized for display).</summary>
	[Display(Name = nameof(DtoResources.ProductionJob_PhaseNumber), ResourceType = typeof(DtoResources))]
	public string? PhaseNumber { get; set; }
}
