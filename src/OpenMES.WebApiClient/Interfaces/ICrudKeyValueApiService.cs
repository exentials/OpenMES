using OpenMES.Data.Dtos;

namespace OpenMES.WebApiClient.Interfaces;

public interface ICrudKeyValueApiService<TDto, TKey> : ICrudApiService<TDto, TKey> where TDto : class
{
	Task<IApiResult<IEnumerable<KeyValueDto<TKey>>>> ReadKeyValuesAsync(KeyValueRequestDto keyValueRequestDto, CancellationToken cancellationToken);
}
