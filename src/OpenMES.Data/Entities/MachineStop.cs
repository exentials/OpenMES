using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using OpenMES.Data.Common;
using OpenMES.Data.Dtos;
using OpenMES.Data.Entities.Interfaces;
using OpenMES.Data.Interfaces;

namespace OpenMES.Data.Entities;

/// <summary>
/// Records a machine stop event — a period during which a machine was not producing.
/// Each stop has a start time and optionally an end time (null if the stop is still active).
///
/// Stops are categorized via <see cref="MachineStopReason"/> which carries a
/// <see cref="MachineStopCategory"/> (Breakdown, Setup, Maintenance, MaterialWaiting,
/// Organizational, Other). This enables OEE (Overall Equipment Effectiveness) reporting.
///
/// A stop can optionally be linked to a <see cref="ProductionOrderPhase"/> to associate
/// the downtime with the order that was interrupted.
///
/// Note: machine stops are distinct from machine state declarations (<c>MachineState</c>).
/// A stop is a discrete downtime event with duration. A state declaration is a point-in-time
/// status change that may or may not result in a stop record.
/// </summary>
[Table(nameof(MachineStop))]
[PrimaryKey(nameof(Id))]
public class MachineStop : IKey<int>, IBaseDates, IDtoAdapter<MachineStop, MachineStopDto>
{
	public int Id { get; set; }
	public int MachineId { get; set; }
	public int? ProductionOrderPhaseId { get; set; }
	public int MachineStopReasonId { get; set; }
	public DateTimeOffset StartDate { get; set; }
	public DateTimeOffset? EndDate { get; set; }
	[StringLength(500)]
	public string? Notes { get; set; }
	public DateTimeOffset CreatedAt { get; set; }
	public DateTimeOffset UpdatedAt { get; set; }

	[ForeignKey(nameof(MachineId))]
	public virtual Machine Machine { get; set; } = null!;

	[ForeignKey(nameof(ProductionOrderPhaseId))]
	public virtual ProductionOrderPhase? ProductionOrderPhase { get; set; }

	[ForeignKey(nameof(MachineStopReasonId))]
	[InverseProperty(nameof(MachineStopReason.MachineStops))]
	public virtual MachineStopReason MachineStopReason { get; set; } = null!;

	public static MachineStopDto AsDto(MachineStop entity) => new()
	{
		Id = entity.Id,
		MachineId = entity.MachineId,
		ProductionOrderPhaseId = entity.ProductionOrderPhaseId,
		MachineStopReasonId = entity.MachineStopReasonId,
		StartDate = entity.StartDate,
		EndDate = entity.EndDate,
		Notes = entity.Notes,
		CreatedAt = entity.CreatedAt,
		UpdatedAt = entity.UpdatedAt,
		MachineCode = entity.Machine?.Code,
		MachineStopReasonCode = entity.MachineStopReason?.Code,
		MachineStopReasonDescription = entity.MachineStopReason?.Description,
		MachineStopReasonCategory = entity.MachineStopReason?.Category
	};

	public static MachineStop AsEntity(MachineStopDto dto) => new()
	{
		Id = dto.Id,
		MachineId = dto.MachineId,
		ProductionOrderPhaseId = dto.ProductionOrderPhaseId,
		MachineStopReasonId = dto.MachineStopReasonId,
		StartDate = dto.StartDate,
		EndDate = dto.EndDate,
		Notes = dto.Notes
	};
}
