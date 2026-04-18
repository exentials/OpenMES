using OpenMES.Data.Common;
using OpenMES.Data.Dtos.Resources;
using System.ComponentModel.DataAnnotations;

namespace OpenMES.Data.Dtos;

public class PhasePickingListDto : IKey<int>
{
	/// <summary>Unique identifier of the picking list line.</summary>
	public int Id { get; set; }

	/// <summary>FK to the production order phase this line belongs to.</summary>
	[Display(Name = nameof(DtoResources.PhasePickingList_ProductionOrderPhaseId), ResourceType = typeof(DtoResources))]
	public int ProductionOrderPhaseId { get; set; }

	/// <summary>FK to the material to be picked.</summary>
	[Display(Name = nameof(DtoResources.PhasePickingList_MaterialId), ResourceType = typeof(DtoResources))]
	public int MaterialId { get; set; }

	/// <summary>FK to the preferred source storage location. Null if not assigned.</summary>
	[Display(Name = nameof(DtoResources.PhasePickingList_StorageLocationId), ResourceType = typeof(DtoResources))]
	public int? StorageLocationId { get; set; }

	/// <summary>Total quantity required. Defaults from BOM, editable per order.</summary>
	[Display(Name = nameof(DtoResources.PhasePickingList_RequiredQuantity), ResourceType = typeof(DtoResources))]
	public decimal RequiredQuantity { get; set; }

	/// <summary>Total quantity already picked across all picking items.</summary>
	[Display(Name = nameof(DtoResources.PhasePickingList_PickedQuantity), ResourceType = typeof(DtoResources))]
	public decimal PickedQuantity { get; set; }

	/// <summary>When true, picking is triggered automatically on production declaration.</summary>
	[Display(Name = nameof(DtoResources.PhasePickingList_IsAutomatic), ResourceType = typeof(DtoResources))]
	public bool IsAutomatic { get; set; }

	/// <summary>
	/// When true, this material is consumable and no physical picking is required.
	/// Defaults from Material.IsConsumable at creation time, overridable per order line.
	/// </summary>
	[Display(Name = nameof(DtoResources.PhasePickingList_IsConsumable), ResourceType = typeof(DtoResources))]
	public bool IsConsumable { get; set; }

	/// <summary>
	/// When true, this is a phantom item used only for BOM grouping — never requires picking.
	/// Defaults from Material.IsPhantom at creation time, overridable per order line.
	/// </summary>
	[Display(Name = nameof(DtoResources.PhasePickingList_IsPhantom), ResourceType = typeof(DtoResources))]
	public bool IsPhantom { get; set; }

	/// <summary>Current picking status: Pending, PartiallyPicked, or Completed.</summary>
	[Display(Name = nameof(DtoResources.PhasePickingList_Status), ResourceType = typeof(DtoResources))]
	public PickingStatus Status { get; set; }

	/// <summary>Free-text notes about this picking line.</summary>
	[StringLength(500)]
	[Display(Name = nameof(DtoResources.PhasePickingList_Notes), ResourceType = typeof(DtoResources))]
	public string? Notes { get; set; }

	/// <summary>Timestamp of record creation (UTC).</summary>
	public DateTimeOffset CreatedAt { get; set; }

	/// <summary>Timestamp of last record update (UTC).</summary>
	public DateTimeOffset UpdatedAt { get; set; }

	// Denormalized for display
	/// <summary>Phase number of the linked production order phase (denormalized for display).</summary>
	[Display(Name = nameof(DtoResources.PhasePickingList_PhaseNumber), ResourceType = typeof(DtoResources))]
	public string? PhaseNumber { get; set; }

	/// <summary>Part number of the material to pick (denormalized for display).</summary>
	[Display(Name = nameof(DtoResources.PhasePickingList_PartNumber), ResourceType = typeof(DtoResources))]
	public string? PartNumber { get; set; }

	/// <summary>Description of the material to pick (denormalized for display).</summary>
	[Display(Name = nameof(DtoResources.PhasePickingList_PartDescription), ResourceType = typeof(DtoResources))]
	public string? PartDescription { get; set; }

	/// <summary>Unit of measure of the material (denormalized for display).</summary>
	[Display(Name = nameof(DtoResources.PhasePickingList_UnitOfMeasure), ResourceType = typeof(DtoResources))]
	public string? UnitOfMeasure { get; set; }

	/// <summary>Code of the source storage location (denormalized for display).</summary>
	[Display(Name = nameof(DtoResources.PhasePickingList_StorageLocationCode), ResourceType = typeof(DtoResources))]
	public string? StorageLocationCode { get; set; }
}
