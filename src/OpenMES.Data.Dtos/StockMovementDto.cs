using OpenMES.Data.Common;
using OpenMES.Data.Dtos.Resources;
using System.ComponentModel.DataAnnotations;

namespace OpenMES.Data.Dtos;

public class StockMovementDto : IKey<int>
{
	/// <summary>Unique identifier of the stock movement. Records are append-only and never updated.</summary>
	public int Id { get; set; }

	/// <summary>FK to the material involved in this movement.</summary>
	[Display(Name = nameof(DtoResources.StockMovement_MaterialId), ResourceType = typeof(DtoResources))]
	public int MaterialId { get; set; }

	/// <summary>FK to the storage location affected by this movement.</summary>
	[Display(Name = nameof(DtoResources.StockMovement_StorageLocationId), ResourceType = typeof(DtoResources))]
	public int StorageLocationId { get; set; }

	/// <summary>Nature of the movement: GoodsReceipt, GoodsIssue, Adjustment, Transfer, or ProductionIssue.</summary>
	[Display(Name = nameof(DtoResources.StockMovement_MovementType), ResourceType = typeof(DtoResources))]
	public StockMovementType MovementType { get; set; }

	/// <summary>Quantity moved. Always positive; direction is determined by MovementType.</summary>
	[Display(Name = nameof(DtoResources.StockMovement_Quantity), ResourceType = typeof(DtoResources))]
	public decimal Quantity { get; set; }

	/// <summary>Date and time the movement was physically executed.</summary>
	[Display(Name = nameof(DtoResources.StockMovement_MovementDate), ResourceType = typeof(DtoResources))]
	public DateTimeOffset MovementDate { get; set; }

	/// <summary>ID of the source document that originated this movement (e.g. ProductionOrder.Id).</summary>
	[Display(Name = nameof(DtoResources.StockMovement_ReferenceId), ResourceType = typeof(DtoResources))]
	public int? ReferenceId { get; set; }

	/// <summary>Type discriminator of the source document (e.g. "ProductionOrder", "PurchaseOrder").</summary>
	[StringLength(50)]
	[Display(Name = nameof(DtoResources.StockMovement_ReferenceType), ResourceType = typeof(DtoResources))]
	public string? ReferenceType { get; set; }

	/// <summary>FK to the operator who registered the movement.</summary>
	[Display(Name = nameof(DtoResources.StockMovement_OperatorId), ResourceType = typeof(DtoResources))]
	public int OperatorId { get; set; }

	/// <summary>Free-text notes about the movement.</summary>
	[StringLength(500)]
	[Display(Name = nameof(DtoResources.StockMovement_Notes), ResourceType = typeof(DtoResources))]
	public string? Notes { get; set; }

	/// <summary>Timestamp of record creation (UTC).</summary>
	public DateTimeOffset CreatedAt { get; set; }

	/// <summary>Timestamp of last record update (UTC).</summary>
	public DateTimeOffset UpdatedAt { get; set; }

	// Denormalized for display
	/// <summary>Part number of the material (denormalized for display).</summary>
	[Display(Name = nameof(DtoResources.StockMovement_PartNumber), ResourceType = typeof(DtoResources))]
	public string? PartNumber { get; set; }

	/// <summary>Description of the material (denormalized for display).</summary>
	[Display(Name = nameof(DtoResources.StockMovement_PartDescription), ResourceType = typeof(DtoResources))]
	public string? PartDescription { get; set; }

	/// <summary>Code of the storage location (denormalized for display).</summary>
	[Display(Name = nameof(DtoResources.StockMovement_StorageLocationCode), ResourceType = typeof(DtoResources))]
	public string? StorageLocationCode { get; set; }

	/// <summary>Full name of the operator who registered the movement (denormalized for display).</summary>
	[Display(Name = nameof(DtoResources.StockMovement_OperatorName), ResourceType = typeof(DtoResources))]
	public string? OperatorName { get; set; }
}
