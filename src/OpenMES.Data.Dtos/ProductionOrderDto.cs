using OpenMES.Data.Common;
using OpenMES.Data.Dtos.Resources;
using System.ComponentModel.DataAnnotations;

namespace OpenMES.Data.Dtos;

public class ProductionOrderDto : IKey<int>
{
	/// <summary>Unique identifier of the production order.</summary>
	public int Id { get; set; }

	/// <summary>FK to the plant where this order is executed.</summary>
	[Display(Name = nameof(DtoResources.ProductionOrder_PlantId), ResourceType = typeof(DtoResources))]
	public int PlantId { get; set; }

	/// <summary>FK to the material to be produced.</summary>
	[Display(Name = nameof(DtoResources.ProductionOrder_MaterialId), ResourceType = typeof(DtoResources))]
	public int MaterialId { get; set; }

	/// <summary>Unique order number, typically imported from the ERP system.</summary>
	[MaxLength(20)]
	[Display(Name = nameof(DtoResources.ProductionOrder_OrderNumber), ResourceType = typeof(DtoResources))]
	public string OrderNumber { get; set; } = null!;

	/// <summary>Type classification of the order (e.g. standard, rework).</summary>
	[MaxLength(20)]
	[Display(Name = nameof(DtoResources.ProductionOrder_OrderType), ResourceType = typeof(DtoResources))]
	public string OrderType { get; set; } = null!;

	/// <summary>Total quantity originally planned for this order.</summary>
	[Display(Name = nameof(DtoResources.ProductionOrder_PlannedQuantity), ResourceType = typeof(DtoResources))]
	public decimal PlannedQuantity { get; set; }

	/// <summary>Total quantity confirmed as good across all phases.</summary>
	[Display(Name = nameof(DtoResources.ProductionOrder_ConfirmedQuantity), ResourceType = typeof(DtoResources))]
	public decimal ConfirmedQuantity { get; set; }

	/// <summary>Total quantity declared as scrap across all phases.</summary>
	[Display(Name = nameof(DtoResources.ProductionOrder_ScrapQuantity), ResourceType = typeof(DtoResources))]
	public decimal ScrapQuantity { get; set; }

	/// <summary>Timestamp of record creation (UTC).</summary>
	public DateTimeOffset CreatedAt { get; set; }

	/// <summary>Timestamp of last record update (UTC).</summary>
	public DateTimeOffset UpdatedAt { get; set; }

	// Denormalized for display
	/// <summary>Name of the plant (denormalized for display).</summary>
	[Display(Name = nameof(DtoResources.ProductionOrder_PlantName), ResourceType = typeof(DtoResources))]
	public string? PlantName { get; set; }

	/// <summary>Part number of the material to produce (denormalized for display).</summary>
	[Display(Name = nameof(DtoResources.ProductionOrder_PartNumber), ResourceType = typeof(DtoResources))]
	public string? PartNumber { get; set; }

	/// <summary>Description of the material to produce (denormalized for display).</summary>
	[Display(Name = nameof(DtoResources.ProductionOrder_PartDescription), ResourceType = typeof(DtoResources))]
	public string? PartDescription { get; set; }

	/// <summary>Ordered list of phases that make up this production order.</summary>
	public ICollection<ProductionOrderPhaseDto> ProductionOrderPhases { get; set; } = [];
}
