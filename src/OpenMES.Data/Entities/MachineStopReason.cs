using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using OpenMES.Data.Common;
using OpenMES.Data.Dtos;
using OpenMES.Data.Entities.Interfaces;
using OpenMES.Data.Interfaces;

namespace OpenMES.Data.Entities;

/// <summary>
/// Master data record describing the reason for a machine stop.
/// Each reason has a short <see cref="Code"/>, a human-readable
/// <see cref="Description"/>, and a <see cref="MachineStopCategory"/> that
/// classifies it for OEE reporting purposes.
///
/// Categories map to standard OEE loss categories:
/// - Breakdown → unplanned availability loss
/// - Setup / Changeover → planned availability loss
/// - Maintenance → planned or unplanned availability loss
/// - MaterialWaiting → organizational loss
/// - Organizational → organizational loss
/// - Other → uncategorized
///
/// The <see cref="Disabled"/> flag hides obsolete reasons from operator selection
/// without deleting historical stop records that reference them.
/// </summary>
[Table(nameof(MachineStopReason))]
[PrimaryKey(nameof(Id))]
public class MachineStopReason : IKey<int>, IBaseDates, IDtoAdapter<MachineStopReason, MachineStopReasonDto>
{
	public int Id { get; set; }
	[Required, StringLength(20)]
	public string Code { get; set; } = null!;
	[StringLength(80)]
	public string Description { get; set; } = null!;
	public MachineStopCategory Category { get; set; }
	public bool Disabled { get; set; }
	public DateTimeOffset CreatedAt { get; set; }
	public DateTimeOffset UpdatedAt { get; set; }

	[InverseProperty(nameof(MachineStop.MachineStopReason))]
	public virtual ICollection<MachineStop> MachineStops { get; set; } = [];

	public static MachineStopReasonDto AsDto(MachineStopReason entity) => new()
	{
		Id = entity.Id,
		Code = entity.Code,
		Description = entity.Description,
		Category = entity.Category,
		Disabled = entity.Disabled,
		CreatedAt = entity.CreatedAt,
		UpdatedAt = entity.UpdatedAt
	};

	public static MachineStopReason AsEntity(MachineStopReasonDto dto) => new()
	{
		Id = dto.Id,
		Code = dto.Code,
		Description = dto.Description,
		Category = dto.Category,
		Disabled = dto.Disabled
	};

}
