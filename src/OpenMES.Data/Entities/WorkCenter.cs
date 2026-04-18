using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using OpenMES.Data.Common;
using OpenMES.Data.Dtos;
using OpenMES.Data.Entities.Interfaces;
using OpenMES.Data.Interfaces;

namespace OpenMES.Data.Entities;

/// <summary>
/// Represents a work center within a plant.
/// A work center is a logical grouping of machines that perform similar operations
/// (e.g. "Turning", "Milling", "Assembly"). It is the scheduling unit above the
/// individual machine level. Production order phases are assigned to a work center,
/// and then optionally placed on a specific machine within that work center.
/// </summary>
[Table(nameof(WorkCenter))]
[PrimaryKey(nameof(Id))]
public class WorkCenter : IKey<int>, IDtoAdapter<WorkCenter, WorkCenterDto>, IBaseDates, IKeyValueDtoAdapter<WorkCenter, WorkCenterDto, int>
{
	[Key]
	public int Id { get; set; }
	public int PlantId { get; set; }
	[Required, MaxLength(10)]
	public string Code { get; set; } = null!;
	[MaxLength(40)]
	public string Description { get; set; } = string.Empty;
	public DateTimeOffset CreatedAt { get; set; }
	public DateTimeOffset UpdatedAt { get; set; }

	[ForeignKey(nameof(PlantId))]
	[InverseProperty(nameof(Plant.WorkCenters))]
	public virtual Plant Plant { get; set; } = null!;
	[InverseProperty(nameof(Machine.WorkCenter))]
	public virtual ICollection<Machine> Machines { get; set; } = [];


	public static WorkCenterDto AsDto(WorkCenter entity) => new()
	{
		Id = entity.Id,
		PlantId = entity.PlantId,
		Code = entity.Code,
		Description = entity.Description,
		CreatedAt = entity.CreatedAt,
		UpdatedAt = entity.UpdatedAt,
		PlantCode = entity.Plant.Code,	
		PlantDescription = entity.Plant.Description
	};

	public static WorkCenter AsEntity(WorkCenterDto dto) => new()
	{
		Id = dto.Id,
		PlantId = dto.PlantId,
		Code = dto.Code,
		Description = dto.Description,
		CreatedAt = dto.CreatedAt,
		UpdatedAt = dto.UpdatedAt
	};

	public static KeyValueDto<int> AsKeyValueDto(WorkCenter entity) => new()
	{
		Id = entity.Id,
		Key = entity.Code,
		Value = entity.Description
	};

}
