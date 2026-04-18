using OpenMES.Data.Common;
using OpenMES.Data.Dtos.Resources;
using System.ComponentModel.DataAnnotations;

namespace OpenMES.Data.Dtos;

public class ProductionOrderPhaseDto : IKey<int>
{
	/// <summary>Unique identifier of the production order phase.</summary>
	public int Id { get; set; }

	/// <summary>FK to the parent production order.</summary>
	[Required]
	[Display(Name = nameof(DtoResources.ProductionOrderPhase_ProductionOrderId), ResourceType = typeof(DtoResources))]
	public int ProductionOrderId { get; set; }

	/// <summary>Phase sequence number within the production order (e.g. 0010, 0020).</summary>
	[Required, StringLength(4)]
	[Display(Name = nameof(DtoResources.ProductionOrderPhase_PhaseNumber), ResourceType = typeof(DtoResources))]
	public string PhaseNumber { get; set; } = null!;

	/// <summary>FK to the work center responsible for executing this phase.</summary>
	[Required]
	[Display(Name = nameof(DtoResources.ProductionOrderPhase_WorkCenterId), ResourceType = typeof(DtoResources))]
	public int WorkCenterId { get; set; }

	/// <summary>Work code or operation code associated with this phase.</summary>
	[StringLength(10)]
	[Display(Name = nameof(DtoResources.ProductionOrderPhase_WorkCode), ResourceType = typeof(DtoResources))]
	public string WorkCode { get; set; } = null!;

	/// <summary>Description of the operation to be performed in this phase.</summary>
	[StringLength(80)]
	[Display(Name = nameof(DtoResources.ProductionOrderPhase_Description), ResourceType = typeof(DtoResources))]
	public string Description { get; set; } = null!;

	/// <summary>Unique confirmation number used to identify this phase on the shop floor terminal.</summary>
	[Required, StringLength(24)]
	[Display(Name = nameof(DtoResources.ProductionOrderPhase_ConfirmNumber), ResourceType = typeof(DtoResources))]
	public string ConfirmNumber { get; set; } = null!;

	/// <summary>Planned start date and time for this phase.</summary>
	[Display(Name = nameof(DtoResources.ProductionOrderPhase_StartDate), ResourceType = typeof(DtoResources))]
	public DateTimeOffset StartDate { get; set; }

	/// <summary>Planned end date and time for this phase.</summary>
	[Display(Name = nameof(DtoResources.ProductionOrderPhase_EndDate), ResourceType = typeof(DtoResources))]
	public DateTimeOffset EndDate { get; set; }

	/// <summary>Timestamp of record creation (UTC).</summary>
	public DateTimeOffset CreatedAt { get; set; }

	/// <summary>Timestamp of last record update (UTC).</summary>
	public DateTimeOffset UpdatedAt { get; set; }

	/// <summary>Total quantity planned for this phase.</summary>
	[Display(Name = nameof(DtoResources.ProductionOrderPhase_PlannedQuantity), ResourceType = typeof(DtoResources))]
	public decimal PlannedQuantity { get; set; }

	/// <summary>Running counter of pieces processed (may include both good and scrap).</summary>
	[Display(Name = nameof(DtoResources.ProductionOrderPhase_CounterQuantity), ResourceType = typeof(DtoResources))]
	public decimal CounterQuantity { get; set; }

	/// <summary>Quantity confirmed as good through production declarations.</summary>
	[Display(Name = nameof(DtoResources.ProductionOrderPhase_ConfirmedQuantity), ResourceType = typeof(DtoResources))]
	public decimal ConfirmedQuantity { get; set; }

	/// <summary>Quantity declared as scrap through production declarations.</summary>
	[Display(Name = nameof(DtoResources.ProductionOrderPhase_ScrapQuantity), ResourceType = typeof(DtoResources))]
	public decimal ScrapQuantity { get; set; }

	/// <summary>Current workflow status of the phase (e.g. Released, InProgress, Completed).</summary>
	[Display(Name = nameof(DtoResources.ProductionOrderPhase_Status), ResourceType = typeof(DtoResources))]
	public OrderStatus Status { get; set; }
}
