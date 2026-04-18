using OpenMES.Data.Common;
using OpenMES.Data.Dtos.Resources;
using System.ComponentModel.DataAnnotations;

namespace OpenMES.Data.Dtos;

public class ProductionDeclarationDto : IKey<int>
{
	/// <summary>Unique identifier of the production declaration.</summary>
	public int Id { get; set; }

	/// <summary>FK to the production order phase this declaration refers to.</summary>
	[Display(Name = nameof(DtoResources.ProductionDeclaration_ProductionOrderPhaseId), ResourceType = typeof(DtoResources))]
	public int ProductionOrderPhaseId { get; set; }

	/// <summary>FK to the operator who made the declaration.</summary>
	[Display(Name = nameof(DtoResources.ProductionDeclaration_OperatorId), ResourceType = typeof(DtoResources))]
	public int OperatorId { get; set; }

	/// <summary>FK to the machine on which the declared production was performed.</summary>
	[Display(Name = nameof(DtoResources.ProductionDeclaration_MachineId), ResourceType = typeof(DtoResources))]
	public int MachineId { get; set; }

	/// <summary>Date and time when the declaration was registered.</summary>
	[Display(Name = nameof(DtoResources.ProductionDeclaration_DeclarationDate), ResourceType = typeof(DtoResources))]
	public DateTimeOffset DeclarationDate { get; set; }

	/// <summary>Number of pieces confirmed as good in this declaration.</summary>
	[Display(Name = nameof(DtoResources.ProductionDeclaration_ConfirmedQuantity), ResourceType = typeof(DtoResources))]
	public decimal ConfirmedQuantity { get; set; }

	/// <summary>Number of pieces declared as scrap in this declaration.</summary>
	[Display(Name = nameof(DtoResources.ProductionDeclaration_ScrapQuantity), ResourceType = typeof(DtoResources))]
	public decimal ScrapQuantity { get; set; }

	/// <summary>Free-text notes about the declaration.</summary>
	[StringLength(500)]
	[Display(Name = nameof(DtoResources.ProductionDeclaration_Notes), ResourceType = typeof(DtoResources))]
	public string? Notes { get; set; }

	// ── ERP export ────────────────────────────────────────────────────────────

	/// <summary>Snapshot of ProductionOrderPhase.ExternalId at declaration creation. Used as the phase key in ERP export.</summary>
	[Display(Name = nameof(DtoResources.ProductionDeclaration_PhaseExternalId), ResourceType = typeof(DtoResources))]
	[StringLength(30)]
	public string? PhaseExternalId { get; set; }

	/// <summary>Counter/ID returned by the ERP confirming acquisition. Null = not yet exported.</summary>
	[Display(Name = nameof(DtoResources.ProductionDeclaration_ExternalCounterId), ResourceType = typeof(DtoResources))]
	[StringLength(50)]
	public string? ExternalCounterId { get; set; }

	/// <summary>When this record was sent to the ERP. Null if not yet exported.</summary>
	[Display(Name = nameof(DtoResources.ProductionDeclaration_ErpExportedAt), ResourceType = typeof(DtoResources))]
	public DateTimeOffset? ErpExportedAt { get; set; }

	/// <summary>True if this record is a reversal (storno) with negated quantities.</summary>
	[Display(Name = nameof(DtoResources.ProductionDeclaration_IsReversal), ResourceType = typeof(DtoResources))]
	public bool IsReversal { get; set; }

	/// <summary>FK to the original declaration this record reverses.</summary>
	[Display(Name = nameof(DtoResources.ProductionDeclaration_ReversalOfId), ResourceType = typeof(DtoResources))]
	public int? ReversalOfId { get; set; }

	/// <summary>FK to the reversal declaration that cancelled this record.</summary>
	[Display(Name = nameof(DtoResources.ProductionDeclaration_ReversedById), ResourceType = typeof(DtoResources))]
	public int? ReversedById { get; set; }

	/// <summary>Timestamp of record creation (UTC).</summary>
	public DateTimeOffset CreatedAt { get; set; }

	/// <summary>Timestamp of last record update (UTC).</summary>
	public DateTimeOffset UpdatedAt { get; set; }

	// Denormalized for display
	/// <summary>Phase number of the linked production order phase (denormalized for display).</summary>
	[Display(Name = nameof(DtoResources.ProductionDeclaration_PhaseNumber), ResourceType = typeof(DtoResources))]
	public string? PhaseNumber { get; set; }

	/// <summary>Full name of the operator who made the declaration (denormalized for display).</summary>
	[Display(Name = nameof(DtoResources.ProductionDeclaration_OperatorName), ResourceType = typeof(DtoResources))]
	public string? OperatorName { get; set; }

	/// <summary>Code of the machine used (denormalized for display).</summary>
	[Display(Name = nameof(DtoResources.ProductionDeclaration_MachineCode), ResourceType = typeof(DtoResources))]
	public string? MachineCode { get; set; }
}
