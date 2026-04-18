using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using OpenMES.Data.Common;
using OpenMES.Data.Dtos;
using OpenMES.Data.Entities.Interfaces;
using OpenMES.Data.Interfaces;

namespace OpenMES.Data.Entities;

/// <summary>
/// Represents a single material line in the picking list (bill of materials) for a
/// production order phase. Defines what material needs to be withdrawn from stock
/// before or during the execution of the phase.
///
/// Multiple lines for the same material are allowed on the same phase
/// (e.g. to pick from different storage locations).
///
/// Each line carries three flags that determine whether physical picking is required:
/// - <see cref="IsAutomatic"/>: picking is triggered automatically when a
///   <see cref="ProductionDeclaration"/> is saved, proportional to declared quantity.
/// - <see cref="IsConsumable"/>: material is consumed in process (e.g. cutting oil,
///   adhesive) — no discrete picking needed.
/// - <see cref="IsPhantom"/>: material is a ghost BOM node used only for grouping
///   sub-components — it has no physical stock and is never picked.
///
/// <see cref="RequiredQuantity"/> defaults from the BOM but can be overridden per order.
/// <see cref="PickedQuantity"/> is updated each time a <see cref="PhasePickingItem"/> is recorded.
/// <see cref="Status"/> is derived from the comparison of the two quantities.
/// </summary>
[Table(nameof(PhasePickingList))]
[PrimaryKey(nameof(Id))]
public class PhasePickingList : IKey<int>, IBaseDates, IDtoAdapter<PhasePickingList, PhasePickingListDto>
{
	public int Id { get; set; }

	/// <summary>FK to the production order phase this picking line belongs to.</summary>
	public int ProductionOrderPhaseId { get; set; }

	/// <summary>FK to the material to be picked.</summary>
	public int MaterialId { get; set; }

	/// <summary>FK to the preferred source storage location. Nullable — can be assigned later.</summary>
	public int? StorageLocationId { get; set; }

	/// <summary>
	/// Total quantity required for this line.
	/// Defaults from the bill of materials but can be overridden per order.
	/// </summary>
	[Precision(9, 3)]
	public decimal RequiredQuantity { get; set; }

	/// <summary>Quantity already picked across all picking items for this line.</summary>
	[Precision(9, 3)]
	public decimal PickedQuantity { get; set; }

	/// <summary>
	/// When true, a stock movement is generated automatically proportional
	/// to the quantity declared on the phase (ProductionDeclaration).
	/// When false, picking must be done manually.
	/// </summary>
	public bool IsAutomatic { get; set; }

	/// <summary>
	/// When true, this material is consumable and no physical picking is required.
	/// Defaults from Material.IsConsumable at creation time but can be overridden per order.
	/// </summary>
	public bool IsConsumable { get; set; }

	/// <summary>
	/// When true, this is a phantom (ghost) item used only for BOM grouping.
	/// It has no physical existence and never requires picking.
	/// Defaults from Material.IsPhantom at creation time but can be overridden per order.
	/// </summary>
	public bool IsPhantom { get; set; }

	/// <summary>Current picking status derived from PickedQuantity vs RequiredQuantity.</summary>
	public PickingStatus Status { get; set; } = PickingStatus.Pending;

	[StringLength(500)]
	public string? Notes { get; set; }

	public DateTimeOffset CreatedAt { get; set; }
	public DateTimeOffset UpdatedAt { get; set; }

	[ForeignKey(nameof(ProductionOrderPhaseId))]
	[InverseProperty(nameof(ProductionOrderPhase.PickingLines))]
	public virtual ProductionOrderPhase ProductionOrderPhase { get; set; } = null!;

	[ForeignKey(nameof(MaterialId))]
	public virtual Material Material { get; set; } = null!;

	[ForeignKey(nameof(StorageLocationId))]
	public virtual StorageLocation? StorageLocation { get; set; }

	[InverseProperty(nameof(PhasePickingItem.PhasePickingList))]
	public virtual ICollection<PhasePickingItem> PickingItems { get; set; } = [];

	public static PhasePickingListDto AsDto(PhasePickingList entity) => new()
	{
		Id = entity.Id,
		ProductionOrderPhaseId = entity.ProductionOrderPhaseId,
		MaterialId = entity.MaterialId,
		StorageLocationId = entity.StorageLocationId,
		RequiredQuantity = entity.RequiredQuantity,
		PickedQuantity = entity.PickedQuantity,
		IsAutomatic = entity.IsAutomatic,
		IsConsumable = entity.IsConsumable,
		IsPhantom = entity.IsPhantom,
		Status = entity.Status,
		Notes = entity.Notes,
		CreatedAt = entity.CreatedAt,
		UpdatedAt = entity.UpdatedAt,
		PhaseNumber = entity.ProductionOrderPhase?.PhaseNumber,
		PartNumber = entity.Material?.PartNumber,
		PartDescription = entity.Material?.PartDescription,
		UnitOfMeasure = entity.Material?.UnitOfMeasure,
		StorageLocationCode = entity.StorageLocation?.Code,
	};

	public static PhasePickingList AsEntity(PhasePickingListDto dto) => new()
	{
		Id = dto.Id,
		ProductionOrderPhaseId = dto.ProductionOrderPhaseId,
		MaterialId = dto.MaterialId,
		StorageLocationId = dto.StorageLocationId,
		RequiredQuantity = dto.RequiredQuantity,
		IsAutomatic = dto.IsAutomatic,
		IsConsumable = dto.IsConsumable,
		IsPhantom = dto.IsPhantom,
		Notes = dto.Notes,
	};
}
