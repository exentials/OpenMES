using OpenMES.Data.Common;
using OpenMES.Data.Dtos.Resources;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace OpenMES.Data.Dtos;

public class ClientDeviceDto : IKey<int>
{
	/// <summary>Unique identifier of the client device (shop floor terminal).</summary>
	public int Id { get; set; }

	/// <summary>FK to the plant where this terminal is installed.</summary>
	[Display(Name = nameof(DtoResources.ClientDevice_PlantId), ResourceType = typeof(DtoResources))]
	public int PlantId { get; set; }

	/// <summary>Unique name identifying this terminal on the network.</summary>
	[Display(Name = nameof(DtoResources.ClientDevice_Name), ResourceType = typeof(DtoResources))]
	public string Name { get; set; } = null!;

	/// <summary>Human-readable description or physical location of the terminal.</summary>
	[Display(Name = nameof(DtoResources.ClientDevice_Description), ResourceType = typeof(DtoResources))]
	public string Description { get; set; } = null!;

	/// <summary>Password used for terminal authentication (stored hashed).</summary>
	public string Password { get; set; } = null!;

	/// <summary>Current authentication token issued to this terminal.</summary>
	public string AuthToken { get; set; } = null!;

	/// <summary>When false, this terminal is not allowed to authenticate or submit data.</summary>
	[Display(Name = nameof(DtoResources.ClientDevice_Enabled), ResourceType = typeof(DtoResources))]
	public bool Enabled { get; set; }

	/// <summary>Timestamp of record creation (UTC).</summary>
	public DateTimeOffset CreatedAt { get; set; }

	/// <summary>Timestamp of last record update (UTC).</summary>
	public DateTimeOffset UpdatedAt { get; set; }

	/// <summary>Machines accessible from this terminal.</summary>
	public IEnumerable<MachineDto> Machines { get; set; } = [];

}
