using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using OpenMES.Data.Common;
using OpenMES.Data.Dtos;
using OpenMES.Data.Entities.Interfaces;

namespace OpenMES.Data.Entities;

/// <summary>
/// Represents the current on-hand stock quantity of a material at a specific storage location.
/// This is a derived/aggregated entity — it is updated automatically every time a
/// <see cref="StockMovement"/> is recorded for the same material/location pair.
///
/// This entity is never created or modified directly by users. It is maintained
/// by the stock movement logic to provide a fast current-stock query without
/// having to sum all historical movements.
///
/// The alternate key (MaterialId, StorageLocationId) ensures one record per
/// material/location combination.
/// </summary>
[Table(nameof(MaterialStock))]
[PrimaryKey(nameof(Id))]
public class MaterialStock : IKey<int>, IDtoAdapter<MaterialStock, MaterialStockDto>
{
	public int Id { get; set; }
	public int MaterialId { get; set; }
	public int StorageLocationId { get; set; }
	[Precision(9, 3)]
	public decimal Quantity { get; set; }
	public DateTimeOffset LastMovementDate { get; set; }

	[ForeignKey(nameof(MaterialId))]
	[InverseProperty(nameof(Material.MaterialStocks))]
	public virtual Material Material { get; set; } = null!;

	[ForeignKey(nameof(StorageLocationId))]
	[InverseProperty(nameof(StorageLocation.MaterialStocks))]
	public virtual StorageLocation StorageLocation { get; set; } = null!;

	public static MaterialStockDto AsDto(MaterialStock entity) => new()
	{
		Id = entity.Id,
		MaterialId = entity.MaterialId,
		StorageLocationId = entity.StorageLocationId,
		Quantity = entity.Quantity,
		LastMovementDate = entity.LastMovementDate,
		PartNumber = entity.Material?.PartNumber,
		PartDescription = entity.Material?.PartDescription,
		UnitOfMeasure = entity.Material?.UnitOfMeasure,
		StorageLocationCode = entity.StorageLocation?.Code,
		Warehouse = entity.StorageLocation?.Warehouse?.Code
	};

	public static MaterialStock AsEntity(MaterialStockDto dto) => new()
	{
		Id = dto.Id,
		MaterialId = dto.MaterialId,
		StorageLocationId = dto.StorageLocationId,
		Quantity = dto.Quantity,
		LastMovementDate = dto.LastMovementDate
	};
}
