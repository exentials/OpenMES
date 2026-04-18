using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using OpenMES.Data.Common;
using OpenMES.Data.Dtos;
using OpenMES.Data.Entities.Interfaces;
using OpenMES.Data.Interfaces;

namespace OpenMES.Data.Entities;

/// <summary>
/// Represents a production order — the main work document that drives shop floor activity.
/// A production order is typically imported from the ERP system and contains the
/// material to be produced, the plant, the planned quantities, and is broken down
/// into one or more <see cref="ProductionOrderPhase"/> records.
///
/// Operators and machines work against production order phases, not directly against
/// the order itself. The order aggregates confirmed and scrap quantities from all its phases.
/// </summary>
[Table(nameof(ProductionOrder))]
[PrimaryKey(nameof(Id))]
public class ProductionOrder : IKey<int>, IDtoAdapter<ProductionOrder, ProductionOrderDto>, IBaseDates
{	
	public int Id { get; set; }
	public int PlantId { get; set; }
	public int MaterialId { get; set; }
	[StringLength(20)]
	public string OrderNumber { get; set; } = null!;
	[StringLength(10)]
	public string OrderType { get; set; } = null!;
	[Precision(9, 3)]
	public decimal PlannedQuantity { get; set; }
	[Precision(9, 3)]
	public decimal ConfirmedQuantity { get; set; }
	[Precision(9, 3)]
	public decimal ScrapQuantity { get; set; }
	public DateTimeOffset CreatedAt { get; set; }
	public DateTimeOffset UpdatedAt { get; set; }

	public OrderStatus Status { get; set; }

	[ForeignKey(nameof(MaterialId))]
	[InverseProperty(nameof(Material.ProductionOrders))]
	public virtual Material Material { get; set; } = null!;

	[ForeignKey(nameof(PlantId))]
	[InverseProperty(nameof(Plant.ProductionOrders))]
	public virtual Plant Plant { get; set; } = null!;

	[InverseProperty(nameof(ProductionOrderPhase.ProductionOrder))]
	public ICollection<ProductionOrderPhase> ProductionOrderPhases { get; set; } = [];

	public static ProductionOrderDto AsDto(ProductionOrder entity) => new()
	{
		Id = entity.Id,
		PlantId = entity.PlantId,
		MaterialId = entity.MaterialId,
		OrderNumber = entity.OrderNumber,
		OrderType = entity.OrderType,
		PlannedQuantity = entity.PlannedQuantity,
		ConfirmedQuantity = entity.ConfirmedQuantity,
		ScrapQuantity = entity.ScrapQuantity,
		CreatedAt = entity.CreatedAt,
		UpdatedAt = entity.UpdatedAt,

		PlantName = entity.Plant?.Code,
		PartNumber = entity.Material?.PartNumber,
		PartDescription = entity.Material?.PartDescription
	};

	public static ProductionOrder AsEntity(ProductionOrderDto dto) => new()
	{
		Id = dto.Id,
		PlantId = dto.PlantId,
		MaterialId = dto.MaterialId,
		OrderNumber = dto.OrderNumber,
		OrderType = dto.OrderType,
		PlannedQuantity = dto.PlannedQuantity,
		ConfirmedQuantity = dto.ConfirmedQuantity,
		ScrapQuantity = dto.ScrapQuantity,
	};
}
