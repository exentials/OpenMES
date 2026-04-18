using OpenMES.Data.Common;
using OpenMES.Data.Dtos.Resources;
using System.ComponentModel.DataAnnotations;

namespace OpenMES.Data.Dtos;

public class StorageLocationDto : IKey<int>
{
	/// <summary>Unique identifier of the storage location.</summary>
	public int Id { get; set; }

	/// <summary>FK to the plant this storage location belongs to.</summary>
	[Display(Name = nameof(DtoResources.StorageLocation_PlantId), ResourceType = typeof(DtoResources))]
	public int PlantId { get; set; }

	/// <summary>FK to the warehouse containing this storage location.</summary>
	[Display(Name = nameof(DtoResources.StorageLocation_WarehouseId), ResourceType = typeof(DtoResources))]
	public int WarehouseId { get; set; }

	/// <summary>Short alphanumeric code uniquely identifying the storage location.</summary>
	[Required, StringLength(20)]
	[Display(Name = nameof(DtoResources.StorageLocation_Code), ResourceType = typeof(DtoResources))]
	public string Code { get; set; } = null!;

	/// <summary>Human-readable description of the storage location.</summary>
	[StringLength(80)]
	[Display(Name = nameof(DtoResources.StorageLocation_Description), ResourceType = typeof(DtoResources))]
	public string Description { get; set; } = null!;

	/// <summary>Optional zone or aisle within the warehouse.</summary>
	[StringLength(20)]
	[Display(Name = nameof(DtoResources.StorageLocation_Zone), ResourceType = typeof(DtoResources))]
	public string? Zone { get; set; }

	/// <summary>Optional shelf or bin identifier within the zone.</summary>
	[StringLength(20)]
	[Display(Name = nameof(DtoResources.StorageLocation_Slot), ResourceType = typeof(DtoResources))]
	public string? Slot { get; set; }

	/// <summary>When true, this location is no longer available for new stock movements.</summary>
	[Display(Name = nameof(DtoResources.StorageLocation_Disabled), ResourceType = typeof(DtoResources))]
	public bool Disabled { get; set; }

	/// <summary>Timestamp of record creation (UTC).</summary>
	public DateTimeOffset CreatedAt { get; set; }

	/// <summary>Timestamp of last record update (UTC).</summary>
	public DateTimeOffset UpdatedAt { get; set; }
}
