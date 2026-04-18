using OpenMES.Data.Common;
using OpenMES.Data.Dtos.Resources;
using System.ComponentModel.DataAnnotations;

namespace OpenMES.Data.Dtos;

public class OperatorShiftDto : IKey<int>
{
	/// <summary>Unique identifier of the shift event.</summary>
	public int Id { get; set; }

	/// <summary>FK to the operator who generated this event.</summary>
	[Display(Name = nameof(DtoResources.OperatorShift_OperatorId), ResourceType = typeof(DtoResources))]
	public int OperatorId { get; set; }

	/// <summary>Type of presence event.</summary>
	[Display(Name = nameof(DtoResources.OperatorShift_EventType), ResourceType = typeof(DtoResources))]
	public OperatorEventType EventType { get; set; }

	/// <summary>Timestamp when the event occurred (UTC).</summary>
	[Display(Name = nameof(DtoResources.OperatorShift_EventTime), ResourceType = typeof(DtoResources))]
	public DateTimeOffset EventTime { get; set; }

	/// <summary>Origin of the event: Manual or Terminal.</summary>
	[Display(Name = nameof(DtoResources.OperatorShift_Source), ResourceType = typeof(DtoResources))]
	[StringLength(20)]
	public string Source { get; set; } = "Manual";

	/// <summary>Optional free-text note.</summary>
	[Display(Name = nameof(DtoResources.OperatorShift_Notes), ResourceType = typeof(DtoResources))]
	[StringLength(500)]
	public string? Notes { get; set; }

	/// <summary>Timestamp of record creation (UTC).</summary>
	public DateTimeOffset CreatedAt { get; set; }

	/// <summary>Timestamp of last record update (UTC).</summary>
	public DateTimeOffset UpdatedAt { get; set; }

	// Denormalized
	/// <summary>Full name of the operator (denormalized for display).</summary>
	[Display(Name = nameof(DtoResources.OperatorShift_OperatorName), ResourceType = typeof(DtoResources))]
	public string? OperatorName { get; set; }
}
