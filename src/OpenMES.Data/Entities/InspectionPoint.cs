using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using OpenMES.Data.Common;
using OpenMES.Data.Dtos;
using OpenMES.Data.Entities.Interfaces;

namespace OpenMES.Data.Entities;

/// <summary>
/// Represents a single characteristic (control point) within an <see cref="InspectionPlan"/>.
/// Each point defines one thing to measure or verify during production.
///
/// The <see cref="MeasureType"/> determines how the value is recorded:
/// - Numeric: a decimal measurement compared against <see cref="NominalValue"/>,
///   <see cref="UpperTolerance"/> and <see cref="LowerTolerance"/>.
/// - Boolean: a pass/fail check (e.g. presence of a feature).
/// - Text: a free-text observation or visual inspection result.
///
/// <see cref="Sequence"/> defines the display order within the plan.
/// <see cref="IsMandatory"/> marks points that must be measured before the phase
/// can be declared complete.
/// </summary>
[Table(nameof(InspectionPoint))]
[PrimaryKey(nameof(Id))]
public class InspectionPoint : IKey<int>, IDtoAdapter<InspectionPoint, InspectionPointDto>
{
	public int Id { get; set; }
	public int InspectionPlanId { get; set; }
	public int Sequence { get; set; }
	[Required, StringLength(80)]
	public string Description { get; set; } = null!;
	public MeasureType MeasureType { get; set; }
	[Precision(18, 6)]
	public decimal? NominalValue { get; set; }
	[Precision(18, 6)]
	public decimal? UpperTolerance { get; set; }
	[Precision(18, 6)]
	public decimal? LowerTolerance { get; set; }
	[StringLength(10)]
	public string? UnitOfMeasure { get; set; }
	public bool IsMandatory { get; set; } = true;

	[ForeignKey(nameof(InspectionPlanId))]
	[InverseProperty(nameof(InspectionPlan.InspectionPoints))]
	public virtual InspectionPlan InspectionPlan { get; set; } = null!;

	[InverseProperty(nameof(InspectionReading.InspectionPoint))]
	public virtual ICollection<InspectionReading> InspectionReadings { get; set; } = [];

	public static InspectionPointDto AsDto(InspectionPoint entity) => new()
	{
		Id = entity.Id,
		InspectionPlanId = entity.InspectionPlanId,
		Sequence = entity.Sequence,
		Description = entity.Description,
		MeasureType = entity.MeasureType,
		NominalValue = entity.NominalValue,
		UpperTolerance = entity.UpperTolerance,
		LowerTolerance = entity.LowerTolerance,
		UnitOfMeasure = entity.UnitOfMeasure,
		IsMandatory = entity.IsMandatory
	};

	public static InspectionPoint AsEntity(InspectionPointDto dto) => new()
	{
		Id = dto.Id,
		InspectionPlanId = dto.InspectionPlanId,
		Sequence = dto.Sequence,
		Description = dto.Description,
		MeasureType = dto.MeasureType,
		NominalValue = dto.NominalValue,
		UpperTolerance = dto.UpperTolerance,
		LowerTolerance = dto.LowerTolerance,
		UnitOfMeasure = dto.UnitOfMeasure,
		IsMandatory = dto.IsMandatory
	};

}
