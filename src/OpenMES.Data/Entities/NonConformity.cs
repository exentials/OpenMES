using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using OpenMES.Data.Common;
using OpenMES.Data.Dtos;
using OpenMES.Data.Entities.Interfaces;
using OpenMES.Data.Interfaces;

namespace OpenMES.Data.Entities;

/// <summary>
/// Represents a non-conformity (NC) report raised during production or quality inspection.
/// An NC can be opened manually by an operator or automatically when an
/// <see cref="InspectionReading"/> result is <c>NonConforming</c>.
///
/// Lifecycle: Open → InProgress → Closed.
/// Closure requires a corrective action description and is recorded with the
/// operator who closed it (<see cref="ClosedByOperatorId"/>) and the closure time.
///
/// NC type indicates the origin (Internal, Customer complaint, Supplier defect).
/// Severity drives prioritization (Low, Medium, High, Critical).
/// </summary>
[Table(nameof(NonConformity))]
[PrimaryKey(nameof(Id))]
public class NonConformity : IKey<int>, IBaseDates, IDtoAdapter<NonConformity, NonConformityDto>
{
	public int Id { get; set; }
	[Required, StringLength(20)]
	public string Code { get; set; } = null!;
	public int ProductionOrderPhaseId { get; set; }
	public int? InspectionReadingId { get; set; }
	[Required, StringLength(500)]
	public string Description { get; set; } = null!;
	public NonConformityType Type { get; set; }
	public NonConformitySeverity Severity { get; set; }
	public NonConformityStatus Status { get; set; } = NonConformityStatus.Open;
	[StringLength(1000)]
	public string? CorrectiveAction { get; set; }
	public DateTimeOffset OpenedAt { get; set; } = DateTimeOffset.UtcNow;
	public DateTimeOffset? ClosedAt { get; set; }
	public int? ClosedByOperatorId { get; set; }
	public DateTimeOffset CreatedAt { get; set; }
	public DateTimeOffset UpdatedAt { get; set; }

	[ForeignKey(nameof(ProductionOrderPhaseId))]
	[InverseProperty(nameof(ProductionOrderPhase.NonConformities))]
	public virtual ProductionOrderPhase ProductionOrderPhase { get; set; } = null!;

	[ForeignKey(nameof(InspectionReadingId))]
	public virtual InspectionReading? InspectionReading { get; set; }

	[ForeignKey(nameof(ClosedByOperatorId))]
	public virtual Operator? ClosedByOperator { get; set; }

	public static NonConformityDto AsDto(NonConformity entity) => new()
	{
		Id = entity.Id,
		Code = entity.Code,
		ProductionOrderPhaseId = entity.ProductionOrderPhaseId,
		InspectionReadingId = entity.InspectionReadingId,
		Description = entity.Description,
		Type = entity.Type,
		Severity = entity.Severity,
		Status = entity.Status,
		CorrectiveAction = entity.CorrectiveAction,
		OpenedAt = entity.OpenedAt,
		ClosedAt = entity.ClosedAt,
		ClosedByOperatorId = entity.ClosedByOperatorId,
		CreatedAt = entity.CreatedAt,
		UpdatedAt = entity.UpdatedAt,
		PhaseNumber = entity.ProductionOrderPhase?.PhaseNumber,
		ClosedByOperatorName = entity.ClosedByOperator?.Name
	};

	public static NonConformity AsEntity(NonConformityDto dto) => new()
	{
		Id = dto.Id,
		Code = dto.Code,
		ProductionOrderPhaseId = dto.ProductionOrderPhaseId,
		InspectionReadingId = dto.InspectionReadingId,
		Description = dto.Description,
		Type = dto.Type,
		Severity = dto.Severity,
		Status = dto.Status,
		CorrectiveAction = dto.CorrectiveAction,
		OpenedAt = dto.OpenedAt,
		ClosedAt = dto.ClosedAt,
		ClosedByOperatorId = dto.ClosedByOperatorId
	};
}
