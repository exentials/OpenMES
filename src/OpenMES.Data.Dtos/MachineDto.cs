using OpenMES.Data.Common;
using OpenMES.Data.Dtos.Resources;
using System.ComponentModel.DataAnnotations;

namespace OpenMES.Data.Dtos;

public class MachineDto : IKey<int>
{
	/// <summary>Unique identifier of the machine.</summary>
	public int Id { get; set; }

	/// <summary>FK to the work center this machine is assigned to.</summary>
	[Display(Name = nameof(DtoResources.Machine_WorkCenterId), ResourceType = typeof(DtoResources))]
	public int WorkCenterId { get; set; }

	/// <summary>Short alphanumeric code uniquely identifying the machine.</summary>
	[Display(Name = nameof(DtoResources.Machine_Code), ResourceType = typeof(DtoResources))]
	public string Code { get; set; } = null!;

	/// <summary>Human-readable description of the machine.</summary>
	[Display(Name = nameof(DtoResources.Machine_Description), ResourceType = typeof(DtoResources))]
	public string Description { get; set; } = string.Empty;

	/// <summary>Machine type or model identifier.</summary>
	[Display(Name = nameof(DtoResources.Machine_Type), ResourceType = typeof(DtoResources))]
	public string Type { get; set; } = string.Empty;

	/// <summary>Current operational status of the machine (Running, Idle, Maintenance, Stopped).</summary>
	[Display(Name = nameof(DtoResources.Machine_Status), ResourceType = typeof(DtoResources))]
	public MachineStatus Status { get; set; }

	/// <summary>When true, phases can be auto-placed on this machine at session open time.</summary>
	[Display(Name = nameof(DtoResources.Machine_Autoplacement), ResourceType = typeof(DtoResources))]
	public bool Autoplacement { get; set; }

	/// <summary>When true, an operator can have multiple open sessions simultaneously on this machine.</summary>
	[Display(Name = nameof(DtoResources.Machine_AllowConcurrentSessions), ResourceType = typeof(DtoResources))]
	public bool AllowConcurrentSessions { get; set; }

	/// <summary>How allocated minutes are distributed among operators who worked on the same phase.</summary>
	[Display(Name = nameof(DtoResources.Machine_TimeAllocationMode), ResourceType = typeof(DtoResources))]
	public MachineTimeAllocationMode TimeAllocationMode { get; set; }

	/// <summary>Timestamp of record creation (UTC).</summary>
	public DateTimeOffset CreatedAt { get; set; }

	/// <summary>Timestamp of last record update (UTC).</summary>
	public DateTimeOffset UpdatedAt { get; set; }

	/// <summary>Returns a color string representing the current machine status for UI display.</summary>
	public string StatusColor => Status switch
	{
		MachineStatus.Running => "green",
		MachineStatus.Idle => "gray",
		MachineStatus.Maintenance => "orange",
		MachineStatus.Stopped => "red",
		_ => "black"
	};

	/// <summary>Work center the machine is assigned to (navigation property).</summary>
	public virtual WorkCenterDto? WorkCenter { get; set; }
}
