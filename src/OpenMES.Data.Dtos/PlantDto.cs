using OpenMES.Data.Common;
using OpenMES.Data.Dtos.Resources;
using System.ComponentModel.DataAnnotations;

namespace OpenMES.Data.Dtos;

public class PlantDto : IKey<int>
{
	/// <summary>Unique identifier of the plant.</summary>
	[Key]
	public int Id { get; set; }

	/// <summary>Short alphanumeric code uniquely identifying the plant.</summary>
	[StringLength(20)]
	[Display(Name = nameof(DtoResources.Plant_Code), ResourceType = typeof(DtoResources))]
	public string Code { get; set; } = null!;

	/// <summary>Human-readable name of the plant.</summary>
	[StringLength(40)]
	[Display(Name = nameof(DtoResources.Plant_Description), ResourceType = typeof(DtoResources))]
	public string Description { get; set; } = null!;

	/// <summary>Timestamp of record creation (UTC).</summary>
	public DateTimeOffset CreatedAt { get; set; }

	/// <summary>Timestamp of last record update (UTC).</summary>
	public DateTimeOffset UpdatedAt { get; set; }
}
