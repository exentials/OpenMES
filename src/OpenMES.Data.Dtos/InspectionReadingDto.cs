using OpenMES.Data.Common;
using OpenMES.Data.Dtos.Resources;
using System.ComponentModel.DataAnnotations;

namespace OpenMES.Data.Dtos;

public class InspectionReadingDto : IKey<int>
{
	/// <summary>Unique identifier of the inspection reading.</summary>
	public int Id { get; set; }

	/// <summary>FK to the inspection point this reading refers to.</summary>
	[Display(Name = nameof(DtoResources.InspectionReading_InspectionPointId), ResourceType = typeof(DtoResources))]
	public int InspectionPointId { get; set; }

	/// <summary>FK to the production order phase during which the reading was taken.</summary>
	[Display(Name = nameof(DtoResources.InspectionReading_ProductionOrderPhaseId), ResourceType = typeof(DtoResources))]
	public int ProductionOrderPhaseId { get; set; }

	/// <summary>FK to the operator who performed the measurement.</summary>
	[Display(Name = nameof(DtoResources.InspectionReading_OperatorId), ResourceType = typeof(DtoResources))]
	public int OperatorId { get; set; }

	/// <summary>Date and time the measurement was recorded.</summary>
	[Display(Name = nameof(DtoResources.InspectionReading_ReadingDate), ResourceType = typeof(DtoResources))]
	public DateTimeOffset ReadingDate { get; set; }

	/// <summary>Measured value when MeasureType is Numeric.</summary>
	[Display(Name = nameof(DtoResources.InspectionReading_NumericValue), ResourceType = typeof(DtoResources))]
	public decimal? NumericValue { get; set; }

	/// <summary>Measured value when MeasureType is Boolean (pass/fail).</summary>
	[Display(Name = nameof(DtoResources.InspectionReading_BooleanValue), ResourceType = typeof(DtoResources))]
	public bool? BooleanValue { get; set; }

	/// <summary>Measured value when MeasureType is Text.</summary>
	[StringLength(200)]
	[Display(Name = nameof(DtoResources.InspectionReading_TextValue), ResourceType = typeof(DtoResources))]
	public string? TextValue { get; set; }

	/// <summary>Outcome of the measurement: Conforming, NonConforming, or ToVerify.</summary>
	[Display(Name = nameof(DtoResources.InspectionReading_Result), ResourceType = typeof(DtoResources))]
	public InspectionResult Result { get; set; }

	/// <summary>Free-text notes about the measurement or observed condition.</summary>
	[StringLength(500)]
	[Display(Name = nameof(DtoResources.InspectionReading_Notes), ResourceType = typeof(DtoResources))]
	public string? Notes { get; set; }

	/// <summary>Timestamp of record creation (UTC).</summary>
	public DateTimeOffset CreatedAt { get; set; }

	/// <summary>Timestamp of last record update (UTC).</summary>
	public DateTimeOffset UpdatedAt { get; set; }

	// Denormalized for display
	/// <summary>Description of the measured inspection point (denormalized for display).</summary>
	[Display(Name = nameof(DtoResources.InspectionReading_PointDescription), ResourceType = typeof(DtoResources))]
	public string? InspectionPointDescription { get; set; }

	/// <summary>Full name of the operator who took the reading (denormalized for display).</summary>
	[Display(Name = nameof(DtoResources.InspectionReading_OperatorName), ResourceType = typeof(DtoResources))]
	public string? OperatorName { get; set; }

	/// <summary>Phase number of the linked production order phase (denormalized for display).</summary>
	[Display(Name = nameof(DtoResources.InspectionReading_PhaseNumber), ResourceType = typeof(DtoResources))]
	public string? PhaseNumber { get; set; }
}
