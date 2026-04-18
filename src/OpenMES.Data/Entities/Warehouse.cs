using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using OpenMES.Data.Common;
using OpenMES.Data.Dtos;
using OpenMES.Data.Entities.Interfaces;
using OpenMES.Data.Interfaces;

namespace OpenMES.Data.Entities;

/// <summary>
/// Represents a warehouse within a plant.
/// Warehouses serve as the top level of the storage location hierarchy:
/// Warehouse → StorageLocation → Material Stock
/// 
/// Each warehouse is assigned to a specific plant and can contain multiple
/// storage locations. Materials are routed to a default warehouse on receipt
/// and can be physically stored in specific storage locations.
/// </summary>
[Table(nameof(Warehouse))]
[PrimaryKey(nameof(Id))]
public class Warehouse : IKey<int>, IBaseDates, IDtoAdapter<Warehouse, WarehouseDto>, IKeyValueDtoAdapter<Warehouse, WarehouseDto,int>
{
	public int Id { get; set; }
	public int PlantId { get; set; }
	[Required, StringLength(20)]
	public string Code { get; set; } = null!;
	[StringLength(80)]
	public string Description { get; set; } = null!;
	public bool Disabled { get; set; }
	public DateTimeOffset CreatedAt { get; set; }
	public DateTimeOffset UpdatedAt { get; set; }

	[ForeignKey(nameof(PlantId))]
	[InverseProperty(nameof(Plant.Warehouses))]
	public virtual Plant Plant { get; set; } = null!;

	[InverseProperty(nameof(StorageLocation.Warehouse))]
	public virtual ICollection<StorageLocation> StorageLocations { get; set; } = [];

	[InverseProperty(nameof(Material.DefaultWarehouse))]
	public virtual ICollection<Material> Materials { get; set; } = [];

	public static WarehouseDto AsDto(Warehouse entity) => new()
	{
		Id = entity.Id,
		PlantId = entity.PlantId,
		Code = entity.Code,
		Description = entity.Description,
		Disabled = entity.Disabled,
		CreatedAt = entity.CreatedAt,
		UpdatedAt = entity.UpdatedAt
	};

	public static Warehouse AsEntity(WarehouseDto dto) => new()
	{
		Id = dto.Id,
		PlantId = dto.PlantId,
		Code = dto.Code,
		Description = dto.Description,
		Disabled = dto.Disabled
	};

	public static KeyValueDto<int> AsKeyValueDto(Warehouse entity) => new()
	{
		Id = entity.Id,
		Key = entity.Code,
		Value = entity.Description
	};
}
