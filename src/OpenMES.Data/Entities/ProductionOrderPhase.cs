using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using OpenMES.Data.Common;
using OpenMES.Data.Dtos;
using OpenMES.Data.Entities.Interfaces;
using OpenMES.Data.Interfaces;

namespace OpenMES.Data.Entities;

/// <summary>
/// Represents a single phase (operation) within a production order.
/// A phase is the unit at which shop floor work is declared and tracked.
///
/// Each phase is assigned to a <see cref="WorkCenter"/> and has a unique
/// <see cref="ExternalId"/> (the confirmation number from the ERP system) that
/// identifies it on the shop floor terminal. Operators open <see cref="WorkSession"/>
/// records against a phase to declare setup, work, wait, or rework activity.
///
/// A phase may be linked to one or more <see cref="ProductionJob"/> records that
/// represent the actual CAM programs or job batches executed on the machine.
/// In the simple case, one phase = one job. In complex cases (e.g. laser nesting),
/// one phase can be covered by multiple jobs, and one job can span multiple phases.
///
/// Quantities are tracked at phase level:
/// - <see cref="PlannedQuantity"/>: what was planned by the ERP
/// - <see cref="CounterQuantity"/>: raw counter from the machine (may include scrap)
/// - <see cref="ConfirmedQuantity"/>: good pieces confirmed via ProductionDeclaration
/// - <see cref="ScrapQuantity"/>: scrapped pieces declared via ProductionDeclaration
/// </summary>
[Table(nameof(ProductionOrderPhase))]
[PrimaryKey(nameof(Id))]
public class ProductionOrderPhase : IKey<int>, IDtoAdapter<ProductionOrderPhase, ProductionOrderPhaseDto>, IBaseDates
{
	public int Id { get; set; }
	public int ProductionOrderId { get; set; }
	[Required, StringLength(4)]
	public string PhaseNumber { get; set; } = null!;
	public int WorkCenterId { get; set; }
	[StringLength(10)]
	public string WorkCode { get; set; } = null!;
	[StringLength(80)]
	public string Description { get; set; } = null!;
	[StringLength(30)]
	public string ExternalId { get; set; } = null!;
	public DateTimeOffset StartDate { get; set; }
	public DateTimeOffset EndDate { get; set; }
	public DateTimeOffset CreatedAt { get; set; }
	public DateTimeOffset UpdatedAt { get; set; }
	[Precision(9, 3)]
	public decimal PlannedQuantity { get; set; }
	[Precision(9, 3)]
	public decimal CounterQuantity { get; set; }
	[Precision(9, 3)]
	public decimal ConfirmedQuantity { get; set; }	
	[Precision(9, 3)]
	public decimal ScrapQuantity { get; set; }
	public OrderStatus Status { get; set; }


	[ForeignKey(nameof(ProductionOrderId))]
	[InverseProperty(nameof(ProductionOrder.ProductionOrderPhases))]
	public virtual ProductionOrder ProductionOrder { get; set; } = null!;
	
	public virtual ICollection<ProductionJob> ProductionJobs { get; set; } = [];

	public virtual WorkCenter WorkCenter { get; set; } = null!;

	[InverseProperty(nameof(ProductionDeclaration.ProductionOrderPhase))]
	public virtual ICollection<ProductionDeclaration> ProductionDeclarations { get; set; } = [];

	[InverseProperty(nameof(InspectionReading.ProductionOrderPhase))]
	public virtual ICollection<InspectionReading> InspectionReadings { get; set; } = [];

	[InverseProperty(nameof(NonConformity.ProductionOrderPhase))]
	public virtual ICollection<NonConformity> NonConformities { get; set; } = [];

	[InverseProperty(nameof(Entities.PhasePickingList.ProductionOrderPhase))]
	public virtual ICollection<PhasePickingList> PickingLines { get; set; } = [];

	[InverseProperty(nameof(WorkSession.ProductionOrderPhase))]
	public virtual ICollection<WorkSession> WorkSessions { get; set; } = [];

	[InverseProperty(nameof(MachinePhasePlacement.ProductionOrderPhase))]
	public virtual ICollection<MachinePhasePlacement> MachinePhasePlacements { get; set; } = [];


	public static ProductionOrderPhaseDto AsDto(ProductionOrderPhase entity) => new()
	{
		Id = entity.Id,
		ProductionOrderId = entity.ProductionOrderId,
		PhaseNumber = entity.PhaseNumber,
		WorkCenterId = entity.WorkCenterId,
		ConfirmNumber = entity.ExternalId,
		WorkCode = entity.WorkCode,
		Description = entity.Description,
		StartDate = entity.StartDate,
		EndDate = entity.EndDate,
		CreatedAt = entity.CreatedAt,
		UpdatedAt = entity.UpdatedAt,
		PlannedQuantity = entity.PlannedQuantity,
		CounterQuantity = entity.CounterQuantity,
		ConfirmedQuantity = entity.ConfirmedQuantity,
		ScrapQuantity = entity.ScrapQuantity,
		Status = entity.Status
	};

	public static ProductionOrderPhase AsEntity(ProductionOrderPhaseDto dto) => new()
	{
		Id = dto.Id,
		ProductionOrderId = dto.ProductionOrderId,
		PhaseNumber = dto.PhaseNumber,
		WorkCenterId = dto.WorkCenterId,
		ExternalId = dto.ConfirmNumber,
		WorkCode = dto.WorkCode,
		Description = dto.Description,
		StartDate = dto.StartDate,
		EndDate = dto.EndDate,
		CreatedAt = dto.CreatedAt,
		UpdatedAt = dto.UpdatedAt,
		PlannedQuantity = dto.PlannedQuantity,
		CounterQuantity = dto.CounterQuantity,
		ConfirmedQuantity = dto.ConfirmedQuantity,
		ScrapQuantity = dto.ScrapQuantity,
		Status = dto.Status
	};
}
