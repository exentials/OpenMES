using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using OpenMES.Data.Common;
using OpenMES.Data.Dtos;
using OpenMES.Data.Entities.Interfaces;
using OpenMES.Data.Interfaces;

namespace OpenMES.Data.Entities;

/// <summary>
/// Represents a client device entity, which serves as a model for devices managed within the system.
/// </summary>
/// <remarks>
/// The <see cref="ClientDevice"/> class provides properties to define the device's identity, description.
/// </remarks>
/// <summary>
/// Represents a shop floor terminal device (touchscreen panel, kiosk, or tablet)
/// installed at a work center or machine.
///
/// A client device authenticates to the API using its <see cref="Password"/> and
/// receives an <see cref="AuthToken"/> that is included in subsequent requests.
/// Operators interact with the system through the device's browser interface (WebClient).
///
/// One device can be associated with multiple machines (via <see cref="ClientMachine"/>),
/// allowing a single terminal to manage activity on a group of nearby machines.
///
/// The <see cref="Enabled"/> flag disables a terminal without deleting its configuration
/// or historical data.
/// </summary>
[Table(nameof(ClientDevice))]
[PrimaryKey(nameof(Id))]
public class ClientDevice : IKey<int>, IBaseDates, IDtoAdapter<ClientDevice, ClientDeviceDto>, IKeyValueDtoAdapter<ClientDevice, ClientDeviceDto, int>
{
	[Key]
	public int Id { get; set; }
	public int PlantId { get; set; }
	[Required, StringLength(10)]
	public string Name { get; set; } = string.Empty;
	[Required, StringLength(40)]
	public string Description { get; set; } = string.Empty;
	[StringLength(20)]
	public string Password { get; set; } = string.Empty;
	[StringLength(32)]
	public string AuthToken { get; set; } = string.Empty;
	public bool Enabled { get; set; }
	public DateTimeOffset CreatedAt { get; set; }
	public DateTimeOffset UpdatedAt { get; set; }

	
	public virtual Plant Plant { get; set; } = null!;
	public virtual ICollection<Machine> Machines { get; set; } = [];

	public static ClientDeviceDto AsDto(ClientDevice entity) => new()
	{
		Id = entity.Id,
		PlantId = entity.PlantId,
		Name = entity.Name,
		Description = entity.Description,
		Password = entity.Password,
		AuthToken = entity.AuthToken,
		Enabled = entity.Enabled,
		CreatedAt = entity.CreatedAt,
		UpdatedAt = entity.UpdatedAt,

		Machines = entity.Machines?.Select(Machine.AsDto) ?? []
	};

	public static ClientDevice AsEntity(ClientDeviceDto dto) => new()
	{
		Id = dto.Id,
		Name = dto.Name,
		Description = dto.Description,
		Password = dto.Password,
		AuthToken = dto.AuthToken,
		Enabled = dto.Enabled,
		CreatedAt = dto.CreatedAt,
		UpdatedAt = dto.UpdatedAt
	};

	public static KeyValueDto<int> AsKeyValueDto(ClientDevice entity) => new()
	{
		Id = entity.Id,
		Key = entity.Name,
		Value = entity.Description
	};
}
