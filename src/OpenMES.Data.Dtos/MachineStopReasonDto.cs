using OpenMES.Data.Common;
using OpenMES.Data.Dtos.Resources;
using System.ComponentModel.DataAnnotations;

namespace OpenMES.Data.Dtos;

public class MachineStopReasonDto : IKey<int>
{
	/// <summary>Unique identifier of the machine stop reason.</summary>
	public int Id { get; set; }

	/// <summary>Short alphanumeric code uniquely identifying the stop reason.</summary>
	[Required, StringLength(10)]
	[Display(Name = nameof(DtoResources.MachineStopReason_Code), ResourceType = typeof(DtoResources))]
	public string Code { get; set; } = null!;

	/// <summary>Human-readable description of the stop reason.</summary>
	[StringLength(80)]
	[Display(Name = nameof(DtoResources.MachineStopReason_Description), ResourceType = typeof(DtoResources))]
	public string Description { get; set; } = null!;

	/// <summary>High-level category the stop reason belongs to (e.g. Breakdown, Setup).</summary>
	[Display(Name = nameof(DtoResources.MachineStopReason_Category), ResourceType = typeof(DtoResources))]
	public MachineStopCategory Category { get; set; }

	/// <summary>When true, this stop reason is no longer available for selection.</summary>
	[Display(Name = nameof(DtoResources.MachineStopReason_Disabled), ResourceType = typeof(DtoResources))]
	public bool Disabled { get; set; }

	/// <summary>Timestamp of record creation (UTC).</summary>
	public DateTimeOffset CreatedAt { get; set; }

	/// <summary>Timestamp of last record update (UTC).</summary>
	public DateTimeOffset UpdatedAt { get; set; }
}
