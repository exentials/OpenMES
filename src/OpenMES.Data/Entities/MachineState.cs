using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using OpenMES.Data.Common;
using OpenMES.Data.Dtos;
using OpenMES.Data.Entities.Interfaces;
using OpenMES.Data.Interfaces;

namespace OpenMES.Data.Entities;

/// <summary>
/// Append-only log of machine status transitions.
/// Each record represents a single state change event on a machine.
/// The current state of a machine is always the most recent record for that machine.
///
/// State transitions can be declared by:
/// - The machine controller itself (Source = "Machine") via the API,
///   e.g. a PLC or SCADA system reporting job start/end.
/// - An operator manually via the terminal (Source = "Manual"),
///   with <see cref="OperatorId"/> identifying who made the declaration.
///
/// State changes drive validation for <see cref="WorkSession"/> opening:
/// - Running / Idle  → any session type can be opened.
/// - Setup           → only Setup sessions allowed; Work is rejected.
/// - Stopped         → no sessions can be opened.
/// - Maintenance     → no sessions can be opened.
///
/// Records are never modified or deleted, preserving a complete audit trail
/// of all machine state history.
/// </summary>
[Table(nameof(MachineState))]
[PrimaryKey(nameof(Id))]
public class MachineState : IKey<int>, IBaseDates, IDtoAdapter<MachineState, MachineStateDto>
{
	public int Id { get; set; }

	/// <summary>FK to the machine whose state changed.</summary>
	public int MachineId { get; set; }

	/// <summary>New state of the machine after this event.</summary>
	public MachineStatus Status { get; set; }

	/// <summary>Timestamp when the state change occurred (UTC).</summary>
	public DateTimeOffset EventTime { get; set; }

	/// <summary>
	/// Origin: "Manual" (declared by an operator) or "Machine" (reported by controller).
	/// </summary>
	[StringLength(20)]
	public string Source { get; set; } = "Manual";

	/// <summary>
	/// FK to the operator who declared the state change.
	/// Null when the machine reported the change automatically (Source = "Machine").
	/// </summary>
	public int? OperatorId { get; set; }

	/// <summary>Optional free-text note about this state change.</summary>
	[StringLength(500)]
	public string? Notes { get; set; }

	public DateTimeOffset CreatedAt { get; set; }
	public DateTimeOffset UpdatedAt { get; set; }

	[ForeignKey(nameof(MachineId))]
	[InverseProperty(nameof(Machine.MachineStates))]
	public virtual Machine Machine { get; set; } = null!;

	[ForeignKey(nameof(OperatorId))]
	public virtual Operator? Operator { get; set; }

	public static MachineStateDto AsDto(MachineState entity) => new()
	{
		Id = entity.Id,
		MachineId = entity.MachineId,
		Status = entity.Status,
		EventTime = entity.EventTime,
		Source = entity.Source,
		OperatorId = entity.OperatorId,
		Notes = entity.Notes,
		CreatedAt = entity.CreatedAt,
		UpdatedAt = entity.UpdatedAt,
		MachineCode = entity.Machine?.Code,
		OperatorName = entity.Operator?.Name,
	};

	public static MachineState AsEntity(MachineStateDto dto) => new()
	{
		Id = dto.Id,
		MachineId = dto.MachineId,
		Status = dto.Status,
		EventTime = dto.EventTime,
		Source = dto.Source,
		OperatorId = dto.OperatorId,
		Notes = dto.Notes,
	};

}
