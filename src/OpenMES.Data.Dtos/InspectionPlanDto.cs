using OpenMES.Data.Common;
using OpenMES.Data.Dtos.Resources;
using System.ComponentModel.DataAnnotations;

namespace OpenMES.Data.Dtos;

public class InspectionPlanDto : IKey<int>
{
	/// <summary>Unique identifier of the inspection plan.</summary>
	public int Id { get; set; }

	/// <summary>Short alphanumeric code uniquely identifying the inspection plan.</summary>
	[Required, StringLength(20)]
	[Display(Name = nameof(DtoResources.InspectionPlan_Code), ResourceType = typeof(DtoResources))]
	public string Code { get; set; } = null!;

	/// <summary>Human-readable description of the inspection plan.</summary>
	[StringLength(80)]
	[Display(Name = nameof(DtoResources.InspectionPlan_Description), ResourceType = typeof(DtoResources))]
	public string Description { get; set; } = null!;

	/// <summary>Part number of the material this plan applies to.</summary>
	[StringLength(20)]
	[Display(Name = nameof(DtoResources.InspectionPlan_PartNumber), ResourceType = typeof(DtoResources))]
	public string PartNumber { get; set; } = null!;

	/// <summary>Revision version of this inspection plan. Increments on each approved change.</summary>
	[Display(Name = nameof(DtoResources.InspectionPlan_Version), ResourceType = typeof(DtoResources))]
	public int Version { get; set; } = 1;

	/// <summary>When true, this plan is no longer available for use on new production runs.</summary>
	[Display(Name = nameof(DtoResources.InspectionPlan_Disabled), ResourceType = typeof(DtoResources))]
	public bool Disabled { get; set; }

	/// <summary>Timestamp of record creation (UTC).</summary>
	public DateTimeOffset CreatedAt { get; set; }

	/// <summary>Timestamp of last record update (UTC).</summary>
	public DateTimeOffset UpdatedAt { get; set; }

	/// <summary>Ordered list of control points defined in this inspection plan.</summary>
	public ICollection<InspectionPointDto> InspectionPoints { get; set; } = [];

	public override string ToString() => $"{Code} v{Version}";

}
