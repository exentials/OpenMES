using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using OpenMES.Data.Common;
using OpenMES.Data.Dtos;
using OpenMES.Data.Entities.Interfaces;
using OpenMES.Data.Interfaces;

namespace OpenMES.Data.Entities;

/// <summary>
/// Represents a physical manufacturing site (factory or plant).
/// A plant is the top-level organizational unit in the system.
/// All work centers, operators, production orders, and storage locations
/// belong to a specific plant.
/// </summary>
[Table(nameof(Plant))]
[PrimaryKey(nameof(Id))]
public class Plant : IKey<int>, IBaseDates, IDtoAdapter<Plant, PlantDto>, IKeyValueDtoAdapter<Plant, PlantDto, int>
{
	[Key]
	public int Id { get; set; }
	[Required, StringLength(20)]
	public string Code { get; set; } = null!;
	[StringLength(40)]
	public string Description { get; set; } = null!;
	public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
	public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

	[InverseProperty(nameof(WorkCenter.Plant))]
	public virtual ICollection<WorkCenter> WorkCenters { get; set; } = [];

	[InverseProperty(nameof(Operator.Plant))]
	public virtual ICollection<Operator> Operators { get; set; } = [];

	[InverseProperty(nameof(Warehouse.Plant))]
	public virtual ICollection<Warehouse> Warehouses { get; set; } = [];

	[InverseProperty(nameof(ProductionOrder.Plant))]
	public virtual ICollection<ProductionOrder> ProductionOrders { get; set; } = [];

	public static PlantDto AsDto(Plant entity) => new()
	{
		Id = entity.Id,
		Code = entity.Code,
		Description = entity.Description,
		CreatedAt = entity.CreatedAt,
		UpdatedAt = entity.UpdatedAt
	};

	public static Plant AsEntity(PlantDto dto) => new()
	{
		Id = dto.Id,
		Code = dto.Code,
		Description = dto.Description
	};

	public static KeyValueDto<int> AsKeyValueDto(Plant entity) => new()
	{
		Id = entity.Id,
		Key = entity.Code,
		Value = entity.Description
	};

}
