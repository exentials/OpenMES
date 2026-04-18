using OpenMES.Data.Dtos;

namespace OpenMES.Data.Entities.Interfaces;

/// <summary>
/// Defines methods to convert between an entity and its corresponding Data Transfer Object (DTO) representation.
/// </summary>
/// <remarks>Implement this interface to provide custom mapping logic between domain entities and their DTOs. This
/// is commonly used to separate internal domain models from external data contracts in applications such as web
/// APIs.</remarks>
/// <typeparam name="TEntity">The type of the entity to be converted.</typeparam>
/// <typeparam name="TDto">The type of the Data Transfer Object (DTO) to be converted.</typeparam>
public interface IDtoAdapter<TEntity, TDto>
{
	/// <summary>
	/// Converts the specified entity to its corresponding data transfer object (DTO) representation.
	/// </summary>
	/// <param name="entity">The entity instance to convert to a DTO. Cannot be null.</param>
	/// <returns>A DTO that represents the specified entity.</returns>
	static abstract TDto AsDto(TEntity entity);
	/// <summary>
	/// Converts the specified data transfer object (DTO) to its corresponding entity type.
	/// </summary>
	/// <remarks>This method provides a standardized way to map a DTO to its entity representation. Implementations
	/// should ensure that all relevant data is transferred appropriately.</remarks>
	/// <param name="dto">The data transfer object to convert to an entity. Cannot be null.</param>
	/// <returns>An instance of the entity type that represents the data contained in the specified DTO.</returns>
	static abstract TEntity AsEntity(TDto dto);
}
