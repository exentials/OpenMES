using OpenMES.Data.Dtos;
using OpenMES.WebApiClient.Interfaces;

namespace OpenMES.WebApiClient;

public class CrudKeyValueApiService<TDto, TKey>(HttpClient httpClient, string requestUri)
	: CrudApiService<TDto, TKey>(httpClient, requestUri), ICrudKeyValueApiService<TDto, TKey> where TDto : class
{
	public async Task<IApiResult<IEnumerable<KeyValueDto<TKey>>>> ReadKeyValuesAsync(KeyValueRequestDto keyValueRequestDto, CancellationToken cancellationToken)
	{
		using var requestMessage = new HttpRequestMessage(HttpMethod.Get, $"{RequestUriApi}/keyvalue");
		if (keyValueRequestDto.Term is not null)
		{
			requestMessage.Headers.Add("x-term", $"{keyValueRequestDto.Term}");
		}
		if (keyValueRequestDto.Limit.HasValue == true)
		{
			requestMessage.Headers.Add("x-limit", $"{keyValueRequestDto.Limit.Value}");
		}
		if (keyValueRequestDto.Filters is not null)
		{
			foreach (var (filterProperty, filterValue) in keyValueRequestDto.Filters)
			{
				requestMessage.Headers.Add($"x-filter-{filterProperty}", $"{filterValue}");
			}
		}

		using var response = await HttpClientApi.SendAsync(requestMessage, cancellationToken).ConfigureAwait(false);
		var content = await IApiService.ReadContentSafeAsync(response, cancellationToken).ConfigureAwait(false);

		if (!response.IsSuccessStatusCode)
			return ApiResult<IEnumerable<KeyValueDto<TKey>>>.Fail(content, (int)response.StatusCode);

		var keyValueDto = default(IEnumerable<KeyValueDto<TKey>>);

		try { keyValueDto = content is not null ? IApiService.Deserialize<IEnumerable<KeyValueDto<TKey>>>(content) : null; } catch { /* ignore */ }
		return ApiResult<IEnumerable<KeyValueDto<TKey>>>.Ok(keyValueDto ?? [], (int)response.StatusCode);
	}
}
