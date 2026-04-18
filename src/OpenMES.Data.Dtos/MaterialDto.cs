using OpenMES.Data.Common;
using OpenMES.Data.Dtos.Resources;
using System.ComponentModel.DataAnnotations;

namespace OpenMES.Data.Dtos;

public class MaterialDto : IKey<int>
{
	/// <summary>Unique identifier of the material.</summary>
	public int Id { get; set; }

	/// <summary>Part number: unique alphanumeric code identifying the material in the ERP/BOM.</summary>
	[StringLength(20)]
	[Display(Name = nameof(DtoResources.Material_PartNumber), ResourceType = typeof(DtoResources))]
	public required string PartNumber { get; set; }

	/// <summary>Human-readable description of the material.</summary>
	[StringLength(40)]
	[Display(Name = nameof(DtoResources.Material_PartDescription), ResourceType = typeof(DtoResources))]
	public required string PartDescription { get; set; }

	/// <summary>Optional classification type of the material (e.g. raw material, semi-finished).</summary>
	[StringLength(20)]
	[Display(Name = nameof(DtoResources.Material_PartType), ResourceType = typeof(DtoResources))]
	public string? PartType { get; set; }

	/// <summary>Optional grouping category for the material (e.g. product family).</summary>
	[StringLength(20)]
	[Display(Name = nameof(DtoResources.Material_PartGroup), ResourceType = typeof(DtoResources))]
	public string? PartGroup { get; set; }

	/// <summary>Unit of measure used for quantities of this material (e.g. pcs, kg, m).</summary>
	[StringLength(3)]
	[Display(Name = nameof(DtoResources.Material_UnitOfMeasure), ResourceType = typeof(DtoResources))]
	public required string UnitOfMeasure { get; set; }

	/// <summary>When true, this material is consumed during production without requiring a physical picking operation.</summary>
	[Display(Name = nameof(DtoResources.Material_IsConsumable), ResourceType = typeof(DtoResources))]
	public bool IsConsumable { get; set; }

	/// <summary>When true, this material is a phantom item used only for BOM grouping — never requires picking.</summary>
	[Display(Name = nameof(DtoResources.Material_IsPhantom), ResourceType = typeof(DtoResources))]
	public bool IsPhantom { get; set; }

	/// <summary>When true, operators may declare more confirmed pieces than the order's PlannedQuantity on the first phase of a production order.</summary>
	[Display(Name = nameof(DtoResources.Material_AllowOverproduction), ResourceType = typeof(DtoResources))]
	public bool AllowOverproduction { get; set; }

	/// <summary>Default warehouse where this material should be routed upon receipt.</summary>
	[Display(Name = nameof(DtoResources.Material_DefaultWarehouseId), ResourceType = typeof(DtoResources))]
	public int? DefaultWarehouseId { get; set; }

	/// <summary>Default storage location for this material within the warehouse.</summary>
	[Display(Name = nameof(DtoResources.Material_DefaultStorageLocationId), ResourceType = typeof(DtoResources))]
	public int? DefaultStorageLocationId { get; set; }

	/// <summary>Timestamp of record creation (UTC).</summary>
	public DateTimeOffset CreatedAt { get; set; }

	/// <summary>Timestamp of last record update (UTC).</summary>
	public DateTimeOffset UpdatedAt { get; set; }
}
