namespace OpenMES.Data.Interfaces;

/// <summary>
/// Defines properties for tracking the creation and last update timestamps of an entity.
/// </summary>
public interface IBaseDates
{
	public DateTimeOffset CreatedAt { get; set; }
	public DateTimeOffset UpdatedAt { get; set; }
}
