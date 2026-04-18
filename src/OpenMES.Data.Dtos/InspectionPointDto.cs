using OpenMES.Data.Common;
using OpenMES.Data.Dtos.Resources;
using System.ComponentModel.DataAnnotations;

namespace OpenMES.Data.Dtos;

public class InspectionPointDto : IKey<int>
{
	/// <summary>Unique identifier of the inspection point.</summary>
	public int Id { get; set; }

	/// <summary>FK to the parent inspection plan.</summary>
	[Display(Name = nameof(DtoResources.InspectionPoint_InspectionPlanId), ResourceType = typeof(DtoResources))]
	public int InspectionPlanId { get; set; }

	/// <summary>Execution order of this point within the inspection plan.</summary>
	[Display(Name = nameof(DtoResources.InspectionPoint_Sequence), ResourceType = typeof(DtoResources))]
	public int Sequence { get; set; }

	/// <summary>Description of the characteristic to be measured or verified.</summary>
	[Required, StringLength(80)]
	[Display(Name = nameof(DtoResources.InspectionPoint_Description), ResourceType = typeof(DtoResources))]
	public string Description { get; set; } = null!;

	/// <summary>Data type of the expected measurement: Numeric, Boolean, or Text.</summary>
	[Display(Name = nameof(DtoResources.InspectionPoint_MeasureType), ResourceType = typeof(DtoResources))]
	public MeasureType MeasureType { get; set; }

	/// <summary>Target value for numeric measurements.</summary>
	[Display(Name = nameof(DtoResources.InspectionPoint_NominalValue), ResourceType = typeof(DtoResources))]
	public decimal? NominalValue { get; set; }

	/// <summary>Maximum allowed positive deviation from the nominal value.</summary>
	[Display(Name = nameof(DtoResources.InspectionPoint_UpperTolerance), ResourceType = typeof(DtoResources))]
	public decimal? UpperTolerance { get; set; }

	/// <summary>Maximum allowed negative deviation from the nominal value.</summary>
	[Display(Name = nameof(DtoResources.InspectionPoint_LowerTolerance), ResourceType = typeof(DtoResources))]
	public decimal? LowerTolerance { get; set; }

	/// <summary>Engineering unit of the measured value (e.g. mm, kg, °C).</summary>
	[StringLength(10)]
	[Display(Name = nameof(DtoResources.InspectionPoint_UnitOfMeasure), ResourceType = typeof(DtoResources))]
	public string? UnitOfMeasure { get; set; }

	/// <summary>When true, this point must be filled in before the phase can be confirmed.</summary>
	[Display(Name = nameof(DtoResources.InspectionPoint_IsMandatory), ResourceType = typeof(DtoResources))]
	public bool IsMandatory { get; set; } = true;
}
