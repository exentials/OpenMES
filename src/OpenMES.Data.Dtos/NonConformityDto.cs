using OpenMES.Data.Common;
using OpenMES.Data.Dtos.Resources;
using System.ComponentModel.DataAnnotations;

namespace OpenMES.Data.Dtos;

public class NonConformityDto : IKey<int>
{
	/// <summary>Unique identifier of the non-conformity record.</summary>
	public int Id { get; set; }

	/// <summary>Unique progressive code assigned to the non-conformity (e.g. NC-2025-001).</summary>
	[Required, StringLength(20)]
	[Display(Name = nameof(DtoResources.NonConformity_Code), ResourceType = typeof(DtoResources))]
	public string Code { get; set; } = null!;

	/// <summary>FK to the production order phase where the non-conformity was detected.</summary>
	[Display(Name = nameof(DtoResources.NonConformity_ProductionOrderPhaseId), ResourceType = typeof(DtoResources))]
	public int ProductionOrderPhaseId { get; set; }

	/// <summary>FK to the inspection reading that triggered this non-conformity. Null if raised manually.</summary>
	[Display(Name = nameof(DtoResources.NonConformity_InspectionReadingId), ResourceType = typeof(DtoResources))]
	public int? InspectionReadingId { get; set; }

	/// <summary>Detailed description of the defect or deviation found.</summary>
	[Required, StringLength(500)]
	[Display(Name = nameof(DtoResources.NonConformity_Description), ResourceType = typeof(DtoResources))]
	public string Description { get; set; } = null!;

	/// <summary>Origin of the non-conformity: Internal, Customer, or Supplier.</summary>
	[Display(Name = nameof(DtoResources.NonConformity_Type), ResourceType = typeof(DtoResources))]
	public NonConformityType Type { get; set; }

	/// <summary>Impact level of the non-conformity: Low, Medium, High, or Critical.</summary>
	[Display(Name = nameof(DtoResources.NonConformity_Severity), ResourceType = typeof(DtoResources))]
	public NonConformitySeverity Severity { get; set; }

	/// <summary>Current workflow status: Open, InProgress, or Closed.</summary>
	[Display(Name = nameof(DtoResources.NonConformity_Status), ResourceType = typeof(DtoResources))]
	public NonConformityStatus Status { get; set; }

	/// <summary>Description of the corrective action taken or planned to prevent recurrence.</summary>
	[StringLength(1000)]
	[Display(Name = nameof(DtoResources.NonConformity_CorrectiveAction), ResourceType = typeof(DtoResources))]
	public string? CorrectiveAction { get; set; }

	/// <summary>Date and time when the non-conformity was opened.</summary>
	[Display(Name = nameof(DtoResources.NonConformity_OpenedAt), ResourceType = typeof(DtoResources))]
	public DateTimeOffset OpenedAt { get; set; }

	/// <summary>Date and time when the non-conformity was closed. Null if still open.</summary>
	[Display(Name = nameof(DtoResources.NonConformity_ClosedAt), ResourceType = typeof(DtoResources))]
	public DateTimeOffset? ClosedAt { get; set; }

	/// <summary>FK to the operator who closed the non-conformity.</summary>
	[Display(Name = nameof(DtoResources.NonConformity_ClosedByOperatorId), ResourceType = typeof(DtoResources))]
	public int? ClosedByOperatorId { get; set; }

	/// <summary>Free-text notes about the non-conformity.</summary>
	[Display(Name = nameof(DtoResources.NonConformity_Notes), ResourceType = typeof(DtoResources))]
	public string? Notes { get; set; }

	/// <summary>Timestamp of record creation (UTC).</summary>
	public DateTimeOffset CreatedAt { get; set; }

	/// <summary>Timestamp of last record update (UTC).</summary>
	public DateTimeOffset UpdatedAt { get; set; }

	// Denormalized for display
	/// <summary>Phase number of the linked production order phase (denormalized for display).</summary>
	[Display(Name = nameof(DtoResources.NonConformity_PhaseNumber), ResourceType = typeof(DtoResources))]
	public string? PhaseNumber { get; set; }

	/// <summary>Full name of the operator who closed the non-conformity (denormalized for display).</summary>
	[Display(Name = nameof(DtoResources.NonConformity_ClosedByOperatorName), ResourceType = typeof(DtoResources))]
	public string? ClosedByOperatorName { get; set; }
}
