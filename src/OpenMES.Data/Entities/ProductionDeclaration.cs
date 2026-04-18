using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using OpenMES.Data.Common;
using OpenMES.Data.Dtos;
using OpenMES.Data.Entities.Interfaces;
using OpenMES.Data.Interfaces;

namespace OpenMES.Data.Entities;

/// <summary>
/// Records a quantity declaration on a production order phase.
/// A declaration represents an operator or machine confirming how many pieces
/// were produced (good) and how many were scrapped during a work session.
///
/// Declarations are cumulative — multiple declarations can be made on the same
/// phase by different operators or machines throughout the day.
/// The phase's <see cref="ProductionOrderPhase.ConfirmedQuantity"/> and
/// <see cref="ProductionOrderPhase.ScrapQuantity"/> are the sum of all declarations.
///
/// When a declaration is saved and the phase has picking lines with
/// <see cref="PhasePickingList.IsAutomatic"/> = true, the system automatically
/// generates <see cref="PhasePickingItem"/> records (and the corresponding
/// <see cref="StockMovement"/> entries) proportional to the declared quantity,
/// provided the picking line is not flagged as <see cref="PhasePickingList.IsConsumable"/>
/// or <see cref="PhasePickingList.IsPhantom"/>.
///
/// ERP export:
/// <see cref="PhaseExternalId"/> is a snapshot of <see cref="ProductionOrderPhase.ExternalId"/>
/// copied at declaration creation time. It is the key the ERP uses to identify the phase.
/// When exported, the ERP returns <see cref="ExternalCounterId"/> to confirm acquisition.
/// Corrections to already-exported records are handled via the reversal pattern:
/// a mirror record with <see cref="IsReversal"/> = true and negated quantities
/// is created, followed by a new corrected record.
/// </summary>
[Table(nameof(ProductionDeclaration))]
[PrimaryKey(nameof(Id))]
public class ProductionDeclaration : IKey<int>, IBaseDates, IDtoAdapter<ProductionDeclaration, ProductionDeclarationDto>
{
	public int Id { get; set; }
	public int ProductionOrderPhaseId { get; set; }
	public int OperatorId { get; set; }
	public int MachineId { get; set; }
	public DateTimeOffset DeclarationDate { get; set; }
	[Precision(9, 3)]
	public decimal ConfirmedQuantity { get; set; }
	[Precision(9, 3)]
	public decimal ScrapQuantity { get; set; }
	[StringLength(500)]
	public string? Notes { get; set; }
	public DateTimeOffset CreatedAt { get; set; }
	public DateTimeOffset UpdatedAt { get; set; }

	// ── ERP export fields ─────────────────────────────────────────────────────

	/// <summary>
	/// Snapshot of <see cref="ProductionOrderPhase.ExternalId"/> at declaration creation time.
	/// This is the confirmation number the ERP assigned to the phase at import.
	/// Included in every export so the ERP can identify which phase the data belongs to.
	/// </summary>
	[StringLength(30)]
	public string? PhaseExternalId { get; set; }

	/// <summary>
	/// Counter or ID returned by the ERP when it successfully acquires this record.
	/// Null until the record has been exported and confirmed by the ERP.
	/// Once set, corrections must use the reversal pattern.
	/// </summary>
	[StringLength(50)]
	public string? ExternalCounterId { get; set; }

	/// <summary>Timestamp when this record was sent to the ERP. Null if not yet exported.</summary>
	public DateTimeOffset? ErpExportedAt { get; set; }

	/// <summary>
	/// When true, this record is a reversal (storno) of a previously exported declaration.
	/// <see cref="ConfirmedQuantity"/> and <see cref="ScrapQuantity"/> are negative
	/// to cancel the original values.
	/// </summary>
	public bool IsReversal { get; set; }

	/// <summary>
	/// FK to the original ProductionDeclaration that this record reverses.
	/// Populated only when <see cref="IsReversal"/> = true.
	/// </summary>
	public int? ReversalOfId { get; set; }

	/// <summary>
	/// FK to the reversal ProductionDeclaration that has cancelled this record.
	/// Populated on the original record after a reversal is created.
	/// </summary>
	public int? ReversedById { get; set; }

	[ForeignKey(nameof(ProductionOrderPhaseId))]
	[InverseProperty(nameof(ProductionOrderPhase.ProductionDeclarations))]
	public virtual ProductionOrderPhase ProductionOrderPhase { get; set; } = null!;

	[ForeignKey(nameof(OperatorId))]
	public virtual Operator Operator { get; set; } = null!;

	[ForeignKey(nameof(MachineId))]
	public virtual Machine Machine { get; set; } = null!;

	public static ProductionDeclarationDto AsDto(ProductionDeclaration entity) => new()
	{
		Id = entity.Id,
		ProductionOrderPhaseId = entity.ProductionOrderPhaseId,
		OperatorId = entity.OperatorId,
		MachineId = entity.MachineId,
		DeclarationDate = entity.DeclarationDate,
		ConfirmedQuantity = entity.ConfirmedQuantity,
		ScrapQuantity = entity.ScrapQuantity,
		Notes = entity.Notes,
		PhaseExternalId = entity.PhaseExternalId,
		ExternalCounterId = entity.ExternalCounterId,
		ErpExportedAt = entity.ErpExportedAt,
		IsReversal = entity.IsReversal,
		ReversalOfId = entity.ReversalOfId,
		ReversedById = entity.ReversedById,
		CreatedAt = entity.CreatedAt,
		UpdatedAt = entity.UpdatedAt,
		PhaseNumber = entity.ProductionOrderPhase?.PhaseNumber,
		OperatorName = entity.Operator?.Name,
		MachineCode = entity.Machine?.Code
	};

	public static ProductionDeclaration AsEntity(ProductionDeclarationDto dto) => new()
	{
		Id = dto.Id,
		ProductionOrderPhaseId = dto.ProductionOrderPhaseId,
		OperatorId = dto.OperatorId,
		MachineId = dto.MachineId,
		DeclarationDate = dto.DeclarationDate,
		ConfirmedQuantity = dto.ConfirmedQuantity,
		ScrapQuantity = dto.ScrapQuantity,
		Notes = dto.Notes,
		PhaseExternalId = dto.PhaseExternalId,
		ExternalCounterId = dto.ExternalCounterId,
		ErpExportedAt = dto.ErpExportedAt,
		IsReversal = dto.IsReversal,
		ReversalOfId = dto.ReversalOfId,
		ReversedById = dto.ReversedById,
	};
}
