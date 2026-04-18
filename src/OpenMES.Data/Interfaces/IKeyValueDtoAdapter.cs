using OpenMES.Data.Dtos;

namespace OpenMES.Data.Interfaces;

public interface IKeyValueDtoAdapter<TEntity, TDto, TKey>
{
	/// <summary>
	/// Converts the specified entity to a key-value data transfer object.
	/// </summary>
	/// <param name="entity">The entity to convert to a key-value representation.</param>
	/// <returns>A KeyValueDto representing the key and value extracted from the specified entity.</returns>
	static abstract KeyValueDto<TKey> AsKeyValueDto(TEntity entity);
}