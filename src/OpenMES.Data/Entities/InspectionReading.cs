using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using OpenMES.Data.Common;
using OpenMES.Data.Dtos;
using OpenMES.Data.Entities.Interfaces;
using OpenMES.Data.Interfaces;

namespace OpenMES.Data.Entities;

/// <summary>
/// Records a single measurement or check performed by an operator against an
/// <see cref="InspectionPoint"/> on a specific production order phase.
///
/// The value is stored in one of three typed fields depending on the point's
/// <see cref="MeasureType"/>: <see cref="NumericValue"/>, <see cref="BooleanValue"/>,
/// or <see cref="TextValue"/>. Only the relevant field is populated.
///
/// <see cref="Result"/> is computed at save time by comparing the measured value
/// against the point's nominal and tolerance values:
/// - Conforming: value is within tolerance.
/// - NonConforming: value is outside tolerance — automatically triggers creation
///   of a <see cref="NonConformity"/> record.
/// - NotApplicable: the check was skipped for a documented reason.
///
/// Readings are immutable after creation. Corrections require a new reading.
/// </summary>
[Table(nameof(InspectionReading))]
[PrimaryKey(nameof(Id))]
public class InspectionReading : IKey<int>, IBaseDates, IDtoAdapter<InspectionReading, InspectionReadingDto>
{
	public int Id { get; set; }
	public int InspectionPointId { get; set; }
	public int ProductionOrderPhaseId { get; set; }
	public int OperatorId { get; set; }
	public DateTimeOffset ReadingDate { get; set; }
	[Precision(18, 6)]
	public decimal? NumericValue { get; set; }
	public bool? BooleanValue { get; set; }
	[StringLength(200)]
	public string? TextValue { get; set; }
	public InspectionResult Result { get; set; }
	[StringLength(500)]
	public string? Notes { get; set; }
	public DateTimeOffset CreatedAt { get; set; }
	public DateTimeOffset UpdatedAt { get; set; }

	[ForeignKey(nameof(InspectionPointId))]
	[InverseProperty(nameof(InspectionPoint.InspectionReadings))]
	public virtual InspectionPoint InspectionPoint { get; set; } = null!;

	[ForeignKey(nameof(ProductionOrderPhaseId))]
	[InverseProperty(nameof(ProductionOrderPhase.InspectionReadings))]
	public virtual ProductionOrderPhase ProductionOrderPhase { get; set; } = null!;

	[ForeignKey(nameof(OperatorId))]
	public virtual Operator Operator { get; set; } = null!;

	public virtual NonConformity? NonConformity { get; set; }

	public static InspectionReadingDto AsDto(InspectionReading entity) => new()
	{
		Id = entity.Id,
		InspectionPointId = entity.InspectionPointId,
		ProductionOrderPhaseId = entity.ProductionOrderPhaseId,
		OperatorId = entity.OperatorId,
		ReadingDate = entity.ReadingDate,
		NumericValue = entity.NumericValue,
		BooleanValue = entity.BooleanValue,
		TextValue = entity.TextValue,
		Result = entity.Result,
		Notes = entity.Notes,
		CreatedAt = entity.CreatedAt,
		UpdatedAt = entity.UpdatedAt,
		InspectionPointDescription = entity.InspectionPoint?.Description,
		OperatorName = entity.Operator?.Name,
		PhaseNumber = entity.ProductionOrderPhase?.PhaseNumber
	};

	public static InspectionReading AsEntity(InspectionReadingDto dto) => new()
	{
		Id = dto.Id,
		InspectionPointId = dto.InspectionPointId,
		ProductionOrderPhaseId = dto.ProductionOrderPhaseId,
		OperatorId = dto.OperatorId,
		ReadingDate = dto.ReadingDate,
		NumericValue = dto.NumericValue,
		BooleanValue = dto.BooleanValue,
		TextValue = dto.TextValue,
		Result = dto.Result,
		Notes = dto.Notes
	};
}
