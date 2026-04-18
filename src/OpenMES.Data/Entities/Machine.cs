using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using OpenMES.Data.Common;
using OpenMES.Data.Dtos;
using OpenMES.Data.Entities.Interfaces;
using OpenMES.Data.Interfaces;

namespace OpenMES.Data.Entities;

/// <summary>
/// Represents a physical machine or resource within a work center.
/// A machine can receive production order phases for execution and can declare
/// its own state (Running, Idle, Setup, Stopped, Maintenance) either automatically
/// via a connected controller (PLC/SCADA) or manually by an operator.
///
/// Key configuration fields:
/// - <see cref="Autoplacement"/>: when true, a production order phase can be
///   automatically placed on this machine at the moment the operator opens a work
///   session, without a prior explicit placement step. Useful for fully automatic
///   machines that only report job start/end via JobId.
///   When false, the phase must be explicitly placed on the machine before any
///   work session can be opened.
///
/// Time allocation fields (used by the declaration system):
/// - <see cref="AllowConcurrentSessions"/>: whether multiple operators can have
///   open work sessions simultaneously on this machine.
/// - <see cref="TimeAllocationMode"/>: how allocated minutes are distributed among
///   operators when more than one has worked on the same phase.
/// </summary>
[Table(nameof(Machine))]
[PrimaryKey(nameof(Id))]
public class Machine : IKey<int>, IDtoAdapter<Machine, MachineDto>, IBaseDates, IKeyValueDtoAdapter<Machine, MachineDto, int>
{
	public int Id { get; set; }
	public int WorkCenterId { get; set; }
	[Required, MaxLength(10)]
	public string Code { get; set; } = null!;
	[Required, MaxLength(40)]
	public string Description { get; set; } = null!;
	[MaxLength(20)]
	public string Type { get; set; } = string.Empty;
	public MachineStatus Status { get; set; }
	public DateTimeOffset CreatedAt { get; set; }
	public DateTimeOffset UpdatedAt { get; set; }

	/// <summary>
	/// When true, a production order phase can be automatically placed on this machine
	/// when an operator opens a work session, without requiring an explicit prior placement.
	/// Useful for automatic machines (e.g. laser cutters, CNC centers) that report
	/// job start/end via JobId — the system resolves the phase from the JobId at runtime.
	/// When false, a manual placement step is required before any work session can be opened.
	/// </summary>
	public bool Autoplacement { get; set; }

	/// <summary>
	/// When true, multiple operators can have open WorkSessions simultaneously on this machine.
	/// When false, opening a new session automatically closes any other open session
	/// for the same operator on this machine.
	/// </summary>
	public bool AllowConcurrentSessions { get; set; }

	/// <summary>
	/// Defines how allocated minutes are distributed among operators when more than one
	/// has worked on the same production order phase on this machine.
	/// See <see cref="MachineTimeAllocationMode"/> for available strategies.
	/// </summary>
	public MachineTimeAllocationMode TimeAllocationMode { get; set; }

	[ForeignKey(nameof(WorkCenterId))]
	[InverseProperty(nameof(WorkCenter.Machines))]
	public virtual WorkCenter WorkCenter { get; set; } = null!;

	[InverseProperty(nameof(MachineState.Machine))]
	public virtual ICollection<MachineState> MachineStates { get; set; } = [];

	[InverseProperty(nameof(MachinePhasePlacement.Machine))]
	public virtual ICollection<MachinePhasePlacement> PhasePlacements { get; set; } = [];

	public static MachineDto AsDto(Machine entity) => new()
	{
		Id = entity.Id,
		WorkCenterId = entity.WorkCenterId,
		Code = entity.Code,
		Description = entity.Description,
		Type = entity.Type,
		Status = entity.Status,
		Autoplacement = entity.Autoplacement,
		AllowConcurrentSessions = entity.AllowConcurrentSessions,
		TimeAllocationMode = entity.TimeAllocationMode,
		CreatedAt = entity.CreatedAt,
		UpdatedAt = entity.UpdatedAt
	};

	public static Machine AsEntity(MachineDto dto) => new()
	{
		Id = dto.Id,
		WorkCenterId = dto.WorkCenterId,
		Code = dto.Code,
		Description = dto.Description,
		Type = dto.Type,
		Status = dto.Status,
		Autoplacement = dto.Autoplacement,
		AllowConcurrentSessions = dto.AllowConcurrentSessions,
		TimeAllocationMode = dto.TimeAllocationMode,
		CreatedAt = dto.CreatedAt,
		UpdatedAt = dto.UpdatedAt
	};

	public static KeyValueDto<int> AsKeyValueDto(Machine entity) => new()
	{
		Id = entity.Id,
		Key = entity.Code,
		Value = entity.Description
	};
}
