using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using OpenMES.Data.Common;
using OpenMES.Data.Dtos;
using OpenMES.Data.Entities.Interfaces;
using OpenMES.Data.Interfaces;

namespace OpenMES.Data.Entities;

/// <summary>
/// Represents a material entity with properties describing its part details, type, group, and unit of measure.
/// </summary>
/// <remarks>
/// This class is used to model materials in the system, including their identification, description, and
/// associated metadata. It supports conversion to and from a Data Transfer Object (DTO) for serialization or external
/// communication.
/// </remarks>
/// <summary>
/// Represents a material (article / part) in the system.
/// Materials are the items that are produced, consumed, or stored.
/// They are typically imported from the ERP system and identified
/// by a unique <see cref="PartNumber"/>.
///
/// A material can be:
/// - A finished product (produced via a <see cref="ProductionOrder"/>).
/// - A raw material or component (consumed during production via <see cref="PhasePickingList"/>).
/// - A consumable (<see cref="IsConsumable"/> = true): used in process without
///   discrete tracking — no picking required (e.g. cutting oil, adhesive, fasteners).
/// - A phantom (<see cref="IsPhantom"/> = true): a ghost BOM node used only
///   to group sub-components in the bill of materials. It has no physical
///   stock and is never picked or produced directly.
///
/// Stock levels are tracked per storage location via <see cref="MaterialStock"/>.
/// All stock changes are recorded as <see cref="StockMovement"/> entries.
/// </summary>
[Table(nameof(Material))]
[PrimaryKey(nameof(Id))]
public class Material : IKey<int>, IDtoAdapter<Material, MaterialDto>, IBaseDates, IKeyValueDtoAdapter<Material, MaterialDto, int>
{
	[Key]
	public int Id { get; set; }
	[StringLength(20)]
	public string PartNumber { get; set; } = null!;
	[StringLength(40)]
	public string PartDescription { get; set; } = null!;
	[StringLength(20)]
	public string PartType { get; set; } = string.Empty;
	[StringLength(20)]
	public string PartGroup { get; set; } = string.Empty;
	[StringLength(3)]
	public string UnitOfMeasure { get; set; } = null!;
	/// <summary>
	/// When true, this material is consumed during production without requiring
	/// a physical picking operation (e.g. oils, adhesives, consumables).
	/// </summary>
	public bool IsConsumable { get; set; }

	/// <summary>
	/// When true, this material is a phantom (ghost) item used only for BOM grouping.
	/// It has no physical existence and never requires picking.
	/// </summary>
	public bool IsPhantom { get; set; }

	/// <summary>
	/// When true, operators may declare more confirmed pieces than the order's
	/// PlannedQuantity on the first phase of a production order.
	/// Only applies to the first phase (lowest PhaseNumber). Subsequent phases
	/// are always capped by the confirmed quantity of the previous phase.
	/// </summary>
	public bool AllowOverproduction { get; set; }

	/// <summary>
	/// Default warehouse where this material should be routed upon receipt.
	/// This is the primary destination warehouse for inbound stock.
	/// </summary>
	public int? DefaultWarehouseId { get; set; }

	/// <summary>
	/// Default storage location for this material within the warehouse.
	/// When set, indicates a fixed/dedicated storage slot for this material.
	/// When null, the material can be stored in any available location.
	/// </summary>
	public int? DefaultStorageLocationId { get; set; }

	public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
	public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

	public virtual ICollection<ProductionOrder> ProductionOrders { get; set; } = null!;

	[ForeignKey(nameof(DefaultWarehouseId))]
	[InverseProperty(nameof(Warehouse.Materials))]
	public virtual Warehouse? DefaultWarehouse { get; set; }

	[ForeignKey(nameof(DefaultStorageLocationId))]
	public virtual StorageLocation? DefaultStorageLocation { get; set; }

	[InverseProperty(nameof(MaterialStock.Material))]
	public virtual ICollection<MaterialStock> MaterialStocks { get; set; } = [];

	[InverseProperty(nameof(StockMovement.Material))]
	public virtual ICollection<StockMovement> StockMovements { get; set; } = [];

	public static MaterialDto AsDto(Material entity) => new()
	{
		Id = entity.Id,
		PartNumber = entity.PartNumber,
		PartDescription = entity.PartDescription,
		PartType = entity.PartType,
		PartGroup = entity.PartGroup,
		UnitOfMeasure = entity.UnitOfMeasure,
		IsConsumable = entity.IsConsumable,
		IsPhantom = entity.IsPhantom,
		AllowOverproduction = entity.AllowOverproduction,
	};

	public static Material AsEntity(MaterialDto dto) => new()
	{
		Id = dto.Id,
		PartNumber = dto.PartNumber,
		PartDescription = dto.PartDescription,
		PartType = dto.PartType ?? string.Empty,
		PartGroup = dto.PartGroup ?? string.Empty,
		UnitOfMeasure = dto.UnitOfMeasure,
		IsConsumable = dto.IsConsumable,
		IsPhantom = dto.IsPhantom,
		AllowOverproduction = dto.AllowOverproduction,
	};

	public static KeyValueDto<int> AsKeyValueDto(Material entity) => new()
	{
		Id = entity.Id,
		Key = entity.PartNumber,
		Value = entity.PartDescription
	};
}
