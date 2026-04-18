using OpenMES.Data.Common;
using OpenMES.Data.Dtos.Resources;
using System.ComponentModel.DataAnnotations;

namespace OpenMES.Data.Dtos;

public class MaterialStockDto : IKey<int>
{
	/// <summary>Unique identifier of the stock record.</summary>
	public int Id { get; set; }

	/// <summary>FK to the material whose stock is tracked.</summary>
	[Display(Name = nameof(DtoResources.MaterialStock_MaterialId), ResourceType = typeof(DtoResources))]
	public int MaterialId { get; set; }

	/// <summary>FK to the storage location where the stock is held.</summary>
	[Display(Name = nameof(DtoResources.MaterialStock_StorageLocationId), ResourceType = typeof(DtoResources))]
	public int StorageLocationId { get; set; }

	/// <summary>Current on-hand quantity at this location. Updated on every stock movement.</summary>
	[Display(Name = nameof(DtoResources.MaterialStock_Quantity), ResourceType = typeof(DtoResources))]
	public decimal Quantity { get; set; }

	/// <summary>Date and time of the last stock movement that updated this record.</summary>
	[Display(Name = nameof(DtoResources.MaterialStock_LastMovementDate), ResourceType = typeof(DtoResources))]
	public DateTimeOffset LastMovementDate { get; set; }

	// Denormalized for display
	/// <summary>Part number of the material (denormalized for display).</summary>
	[Display(Name = nameof(DtoResources.MaterialStock_PartNumber), ResourceType = typeof(DtoResources))]
	public string? PartNumber { get; set; }

	/// <summary>Description of the material (denormalized for display).</summary>
	[Display(Name = nameof(DtoResources.MaterialStock_PartDescription), ResourceType = typeof(DtoResources))]
	public string? PartDescription { get; set; }

	/// <summary>Unit of measure of the material (denormalized for display).</summary>
	[Display(Name = nameof(DtoResources.MaterialStock_UnitOfMeasure), ResourceType = typeof(DtoResources))]
	public string? UnitOfMeasure { get; set; }

	/// <summary>Code of the storage location (denormalized for display).</summary>
	[Display(Name = nameof(DtoResources.MaterialStock_StorageLocationCode), ResourceType = typeof(DtoResources))]
	public string? StorageLocationCode { get; set; }

	/// <summary>Warehouse name of the storage location (denormalized for display).</summary>
	[Display(Name = nameof(DtoResources.MaterialStock_Warehouse), ResourceType = typeof(DtoResources))]
	public string? Warehouse { get; set; }
}
