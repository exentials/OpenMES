using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using OpenMES.Data.Common;
using OpenMES.Data.Dtos;
using OpenMES.Data.Entities.Interfaces;
using OpenMES.Data.Interfaces;

namespace OpenMES.Data.Entities;

/// <summary>
/// Records a single stock movement — a change in the quantity of a material
/// at a specific storage location. This is the immutable ledger of all
/// warehouse activity in the system.
///
/// Movement types (<see cref="StockMovementType"/>):
/// - GoodsReceipt: incoming material from supplier or transfer.
/// - ProductionIssue: material issued to a production order phase (generated
///   automatically by <see cref="PhasePickingItem"/> creation).
/// - ProductionReturn: unused material returned to stock after a phase.
/// - Adjustment: manual correction to align system stock with physical count.
/// - Transfer: movement between two storage locations.
/// - Scrap: material scrapped and removed from stock.
///
/// Records are never modified or deleted. Corrections are made by issuing
/// a new movement of type Adjustment with the corrective quantity.
///
/// <see cref="ReferenceType"/> and <see cref="ReferenceId"/> provide a generic
/// foreign key to the originating entity (e.g. "PhasePickingItem" / 42).
/// This avoids a hard FK dependency but allows tracing each movement back
/// to its source.
///
/// <see cref="MaterialStock"/> is updated automatically on each movement
/// to reflect the new on-hand quantity.
/// </summary>
[Table(nameof(StockMovement))]
[PrimaryKey(nameof(Id))]
public class StockMovement : IKey<int>, IBaseDates, IDtoAdapter<StockMovement, StockMovementDto>
{
	public int Id { get; set; }
	public int MaterialId { get; set; }
	public int StorageLocationId { get; set; }
	public StockMovementType MovementType { get; set; }
	[Precision(9, 3)]
	public decimal Quantity { get; set; }
	public DateTimeOffset MovementDate { get; set; }
	public int? ReferenceId { get; set; }
	[StringLength(50)]
	public string? ReferenceType { get; set; }
	public int OperatorId { get; set; }
	[StringLength(500)]
	public string? Notes { get; set; }
	public DateTimeOffset CreatedAt { get; set; }
	public DateTimeOffset UpdatedAt { get; set; }

	[ForeignKey(nameof(MaterialId))]
	[InverseProperty(nameof(Material.StockMovements))]
	public virtual Material Material { get; set; } = null!;

	[ForeignKey(nameof(StorageLocationId))]
	[InverseProperty(nameof(StorageLocation.StockMovements))]
	public virtual StorageLocation StorageLocation { get; set; } = null!;

	[ForeignKey(nameof(OperatorId))]
	public virtual Operator Operator { get; set; } = null!;

	public static StockMovementDto AsDto(StockMovement entity) => new()
	{
		Id = entity.Id,
		MaterialId = entity.MaterialId,
		StorageLocationId = entity.StorageLocationId,
		MovementType = entity.MovementType,
		Quantity = entity.Quantity,
		MovementDate = entity.MovementDate,
		ReferenceId = entity.ReferenceId,
		ReferenceType = entity.ReferenceType,
		OperatorId = entity.OperatorId,
		Notes = entity.Notes,
		CreatedAt = entity.CreatedAt,
		UpdatedAt = entity.UpdatedAt,
		PartNumber = entity.Material?.PartNumber,
		PartDescription = entity.Material?.PartDescription,
		StorageLocationCode = entity.StorageLocation?.Code,
		OperatorName = entity.Operator?.Name
	};

	public static StockMovement AsEntity(StockMovementDto dto) => new()
	{
		Id = dto.Id,
		MaterialId = dto.MaterialId,
		StorageLocationId = dto.StorageLocationId,
		MovementType = dto.MovementType,
		Quantity = dto.Quantity,
		MovementDate = dto.MovementDate,
		ReferenceId = dto.ReferenceId,
		ReferenceType = dto.ReferenceType,
		OperatorId = dto.OperatorId,
		Notes = dto.Notes
	};

}
