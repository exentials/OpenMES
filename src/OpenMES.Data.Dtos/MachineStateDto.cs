using OpenMES.Data.Common;
using OpenMES.Data.Dtos.Resources;
using System.ComponentModel.DataAnnotations;

namespace OpenMES.Data.Dtos;

public class MachineStateDto : IKey<int>
{
	/// <summary>Unique identifier of the state event.</summary>
	public int Id { get; set; }

	/// <summary>FK to the machine whose state changed.</summary>
	[Display(Name = nameof(DtoResources.MachineState_MachineId), ResourceType = typeof(DtoResources))]
	public int MachineId { get; set; }

	/// <summary>New machine status after this event.</summary>
	[Display(Name = nameof(DtoResources.MachineState_Status), ResourceType = typeof(DtoResources))]
	public MachineStatus Status { get; set; }

	/// <summary>Timestamp when the state change occurred (UTC).</summary>
	[Display(Name = nameof(DtoResources.MachineState_EventTime), ResourceType = typeof(DtoResources))]
	public DateTimeOffset EventTime { get; set; }

	/// <summary>Origin: Manual (operator) or Machine (controller).</summary>
	[Display(Name = nameof(DtoResources.MachineState_Source), ResourceType = typeof(DtoResources))]
	[StringLength(20)]
	public string Source { get; set; } = "Manual";

	/// <summary>FK to the operator who declared the change. Null if machine-originated.</summary>
	[Display(Name = nameof(DtoResources.MachineState_OperatorId), ResourceType = typeof(DtoResources))]
	public int? OperatorId { get; set; }

	/// <summary>Optional free-text note.</summary>
	[Display(Name = nameof(DtoResources.MachineState_Notes), ResourceType = typeof(DtoResources))]
	[StringLength(500)]
	public string? Notes { get; set; }

	/// <summary>Timestamp of record creation (UTC).</summary>
	public DateTimeOffset CreatedAt { get; set; }

	/// <summary>Timestamp of last record update (UTC).</summary>
	public DateTimeOffset UpdatedAt { get; set; }

	// Denormalized
	/// <summary>Machine code (denormalized for display).</summary>
	[Display(Name = nameof(DtoResources.MachineState_MachineCode), ResourceType = typeof(DtoResources))]
	public string? MachineCode { get; set; }

	/// <summary>Operator name (denormalized for display).</summary>
	[Display(Name = nameof(DtoResources.MachineState_OperatorName), ResourceType = typeof(DtoResources))]
	public string? OperatorName { get; set; }
}
