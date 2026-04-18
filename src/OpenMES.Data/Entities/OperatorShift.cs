using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OpenMES.Data.Common;
using OpenMES.Data.Dtos;
using OpenMES.Data.Entities.Interfaces;
using OpenMES.Data.Interfaces;

namespace OpenMES.Data.Entities;

/// <summary>
/// Records a single presence event for a shop floor operator.
/// The sequence of events for a given operator and day forms their complete
/// shift timeline (CheckIn → BreakStart → BreakEnd → ... → CheckOut).
///
/// Current operator state is derived by reading the most recent event:
/// - Last event is CheckIn or BreakEnd  → operator is Present
/// - Last event is BreakStart           → operator is On Break
/// - Last event is CheckOut or no event → operator is Absent
///
/// An operator must be Present to open a <see cref="WorkSession"/>.
/// On CheckOut, all open WorkSessions for this operator are force-closed
/// with EndTime set to this event's <see cref="EventTime"/>.
///
/// Records are append-only — errors are corrected by inserting a new event,
/// not by modifying existing ones.
/// </summary>
[Table(nameof(OperatorShift))]
[Microsoft.EntityFrameworkCore.PrimaryKey(nameof(Id))]
public class OperatorShift : IKey<int>, IBaseDates, IDtoAdapter<OperatorShift, OperatorShiftDto>
{
	public int Id { get; set; }

	/// <summary>FK to the operator who generated this event.</summary>
	public int OperatorId { get; set; }

	/// <summary>Type of presence event (CheckIn / CheckOut / BreakStart / BreakEnd).</summary>
	public OperatorEventType EventType { get; set; }

	/// <summary>Timestamp when the event occurred (UTC).</summary>
	public DateTimeOffset EventTime { get; set; }

	/// <summary>
	/// Origin of the event: "Manual" (operator via terminal) or "Terminal" (auto-detected
	/// by the device, e.g. badge scan).
	/// </summary>
	[StringLength(20)]
	public string Source { get; set; } = "Manual";

	/// <summary>Optional free-text note attached to this event.</summary>
	[StringLength(500)]
	public string? Notes { get; set; }

	public DateTimeOffset CreatedAt { get; set; }
	public DateTimeOffset UpdatedAt { get; set; }

	[ForeignKey(nameof(OperatorId))]
	public virtual Operator Operator { get; set; } = null!;

	public static OperatorShiftDto AsDto(OperatorShift entity) => new()
	{
		Id = entity.Id,
		OperatorId = entity.OperatorId,
		EventType = entity.EventType,
		EventTime = entity.EventTime,
		Source = entity.Source,
		Notes = entity.Notes,
		CreatedAt = entity.CreatedAt,
		UpdatedAt = entity.UpdatedAt,
		OperatorName = entity.Operator?.Name,
	};

	public static OperatorShift AsEntity(OperatorShiftDto dto) => new()
	{
		Id = dto.Id,
		OperatorId = dto.OperatorId,
		EventType = dto.EventType,
		EventTime = dto.EventTime,
		Source = dto.Source,
		Notes = dto.Notes,
	};
}
