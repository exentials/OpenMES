using OpenMES.Data.Common;

namespace OpenMES.Data.Dtos;

/// <summary>
/// Data Transfer Object for Warehouse entity.
/// </summary>
public class WarehouseDto : IKey<int>
{
	public int Id { get; set; }
	public int PlantId { get; set; }
	public string Code { get; set; } = null!;
	public string Description { get; set; } = null!;
	public bool Disabled { get; set; }
	public DateTimeOffset CreatedAt { get; set; }
	public DateTimeOffset UpdatedAt { get; set; }
}
