using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using OpenMES.Data.Common;
using OpenMES.Data.Dtos;
using OpenMES.Data.Entities.Interfaces;
using OpenMES.Data.Interfaces;

namespace OpenMES.Data.Entities;

/// <summary>
/// Represents a physical storage location in the warehouse.
/// Locations are organized in a three-level hierarchy:
/// <see cref="Warehouse"/> → StorageLocation (Zone/Slot) → Material Stock
///
/// Each location has a unique <see cref="Code"/> within its warehouse and holds
/// stock for one or more materials (tracked via <see cref="MaterialStock"/>).
/// All stock movements (<see cref="StockMovement"/>) reference a source or
/// destination location.
///
/// The <see cref="Disabled"/> flag removes a location from selection without
/// deleting its historical stock data.
/// </summary>
[Table(nameof(StorageLocation))]
[PrimaryKey(nameof(Id))]
public class StorageLocation : IKey<int>, IBaseDates, IDtoAdapter<StorageLocation, StorageLocationDto>
{
	public int Id { get; set; }
	public int WarehouseId { get; set; }
	[Required, StringLength(20)]
	public string Code { get; set; } = null!;
	[StringLength(80)]
	public string Description { get; set; } = null!;
	[StringLength(20)]
	public string? Zone { get; set; }
	[StringLength(20)]
	public string? Slot { get; set; }
	public bool Disabled { get; set; }
	public DateTimeOffset CreatedAt { get; set; }
	public DateTimeOffset UpdatedAt { get; set; }

	[ForeignKey(nameof(WarehouseId))]
	[InverseProperty(nameof(Warehouse.StorageLocations))]
	public virtual Warehouse Warehouse { get; set; } = null!;

	[InverseProperty(nameof(MaterialStock.StorageLocation))]
	public virtual ICollection<MaterialStock> MaterialStocks { get; set; } = [];

	[InverseProperty(nameof(StockMovement.StorageLocation))]
	public virtual ICollection<StockMovement> StockMovements { get; set; } = [];

	public static StorageLocationDto AsDto(StorageLocation entity) => new()
	{
		Id = entity.Id,
		WarehouseId = entity.WarehouseId,
		Code = entity.Code,
		Description = entity.Description,
		Zone = entity.Zone,
		Slot = entity.Slot,
		Disabled = entity.Disabled,
		CreatedAt = entity.CreatedAt,
		UpdatedAt = entity.UpdatedAt
	};

	public static StorageLocation AsEntity(StorageLocationDto dto) => new()
	{
		Id = dto.Id,
		WarehouseId = dto.WarehouseId,
		Code = dto.Code,
		Description = dto.Description,
		Zone = dto.Zone,
		Slot = dto.Slot,
		Disabled = dto.Disabled
	};
}
