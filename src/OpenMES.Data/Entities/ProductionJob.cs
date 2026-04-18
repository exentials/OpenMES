using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using OpenMES.Data.Common;
using OpenMES.Data.Dtos;
using OpenMES.Data.Entities.Interfaces;
using OpenMES.Data.Interfaces;

namespace OpenMES.Data.Entities;

/// <summary>
/// Represents a job — the execution unit that links a CAM program or machine program
/// to one or more production order phases.
///
/// A <see cref="ProductionJob"/> has a <see cref="JobId"/> that corresponds to the
/// program identifier on the machine controller (e.g. the NC program name on a CNC,
/// or the nesting program name on a laser cutter).
///
/// Relationship with phases:
/// - In the simple case, one phase = one job, and <see cref="JobId"/> equals
///   <see cref="ProductionOrderPhase.ExternalId"/>.
/// - In complex cases (nesting, multi-part fixtures):
///   • One job can cover multiple phases: a laser nesting program processes
///     partial quantities from several different phases on a single sheet.
///   • One phase can be covered by multiple jobs: different nesting runs
///     each contribute partial quantities to the same phase.
///
/// When a machine declares the start or end of a <see cref="JobId"/>, the system
/// resolves the associated phase(s) and generates the corresponding
/// <see cref="WorkSession"/> and <see cref="ProductionDeclaration"/> records automatically.
///
/// Planning fields:
/// - <see cref="PlannedQuantity"/>: how many pieces this job is expected to produce.
/// - <see cref="PlannedSetupTime"/>: expected setup duration in minutes.
/// - <see cref="PlannedRunTime"/>: expected run duration in minutes.
/// </summary>
[Table(nameof(ProductionJob))]
[PrimaryKey(nameof(Id))]
public class ProductionJob : IKey<int>, IBaseDates, IDtoAdapter<ProductionJob, ProductionJobDto>
{
	public int Id { get; set; }
	public int ProductionOrderPhaseId { get; set; }
	[StringLength(30)]
	public string JobId { get; set; } = null!;
	[Precision(9, 3)]
	public decimal PlannedQuantity { get; set; }
	[Precision(9, 3)]
	public decimal PlannedSetupTime { get; set; }
	[Precision(9, 3)]
	public decimal PlannedRunTime { get; set; }
	public DateTimeOffset CreatedAt { get; set; }
	public DateTimeOffset UpdatedAt { get; set; }
	
	public virtual ProductionOrderPhase ProductionOrderPhase { get; set; } = null!;

	public static ProductionJobDto AsDto(ProductionJob entity) => new()
	{
		Id                       = entity.Id,
		ProductionOrderPhaseId   = entity.ProductionOrderPhaseId,
		JobId                    = entity.JobId,
		PlannedQuantity          = entity.PlannedQuantity,
		PlannedSetupTime         = entity.PlannedSetupTime,
		PlannedRunTime           = entity.PlannedRunTime,
		CreatedAt                = entity.CreatedAt,
		UpdatedAt                = entity.UpdatedAt,
		PhaseNumber              = entity.ProductionOrderPhase?.PhaseNumber,
	};

	public static ProductionJob AsEntity(ProductionJobDto dto) => new()
	{
		Id                       = dto.Id,
		ProductionOrderPhaseId   = dto.ProductionOrderPhaseId,
		JobId                    = dto.JobId,
		PlannedQuantity          = dto.PlannedQuantity,
		PlannedSetupTime         = dto.PlannedSetupTime,
		PlannedRunTime           = dto.PlannedRunTime,
		CreatedAt                = dto.CreatedAt,
		UpdatedAt                = dto.UpdatedAt,
	};
}
