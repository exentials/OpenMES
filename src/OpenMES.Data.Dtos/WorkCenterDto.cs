using OpenMES.Data.Common;
using OpenMES.Data.Dtos.Resources;
using System.ComponentModel.DataAnnotations;

namespace OpenMES.Data.Dtos;

public class WorkCenterDto : IKey<int>
{
	/// <summary>Unique identifier of the work center.</summary>
	[Key]
	public int Id { get; set; }

	/// <summary>FK to the plant this work center belongs to.</summary>
	[Display(Name = nameof(DtoResources.WorkCenter_PlantId), ResourceType = typeof(DtoResources))]
	public int PlantId { get; set; }

	/// <summary>Short alphanumeric code uniquely identifying the work center.</summary>
	[Required, MaxLength(10)]
	[Display(Name = nameof(DtoResources.WorkCenter_Code), ResourceType = typeof(DtoResources))]
	public string Code { get; set; } = null!;

	/// <summary>Human-readable description of the work center.</summary>
	[MaxLength(40)]
	[Display(Name = nameof(DtoResources.WorkCenter_Description), ResourceType = typeof(DtoResources))]
	public string Description { get; set; } = string.Empty;

	/// <summary>Timestamp of record creation (UTC).</summary>
	public DateTimeOffset CreatedAt { get; set; }

	/// <summary>Timestamp of last record update (UTC).</summary>
	public DateTimeOffset UpdatedAt { get; set; }

	/// <summary>Machines assigned to this work center.</summary>
	public ICollection<MachineDto> Machines { get; set; } = [];
	/// <summary>Gets or sets the code that uniquely identifies the plant.</summary>
	[Display(Name = nameof(DtoResources.Plant_Code), ResourceType = typeof(DtoResources))]
	public string? PlantCode { get; set; }
	/// <summary>Gets or sets the description of the plant.</summary>
	[Display(Name = nameof(DtoResources.Plant_Description), ResourceType = typeof(DtoResources))]
	public string? PlantDescription { get; set; }
}
