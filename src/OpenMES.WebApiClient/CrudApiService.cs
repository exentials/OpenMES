using OpenMES.Data.Dtos;
using OpenMES.WebApiClient.Interfaces;
using System.Net;
using System.Net.Http.Json;

namespace OpenMES.WebApiClient;

public class CrudApiService<TDto, TKey>(HttpClient httpClient, string requestUri) : ApiService(httpClient, requestUri), ICrudApiService<TDto, TKey> where TDto : class
{
	public async Task<IApiResult<TDto>> CreateAsync(TDto data, CancellationToken cancellationToken = default)
	{
		if (data is null) return ApiResult<TDto>.Fail("Data is null", 400);

		using var response = await HttpClientApi.PostAsJsonAsync(RequestUriApi, data, cancellationToken).ConfigureAwait(false);
		var content = await IApiService.ReadContentSafeAsync(response, cancellationToken).ConfigureAwait(false);

		if (!response.IsSuccessStatusCode)
			return ApiResult<TDto>.Fail(IApiService.ExtractErrorMessage(content), (int)response.StatusCode);

		var dto = default(TDto);
		try { dto = content is not null ? IApiService.Deserialize<TDto>(content) : null; } catch { /* ignore */ }
		return ApiResult<TDto>.Ok(dto, (int)response.StatusCode);
	}

	public async Task<IApiResult> DeleteAsync(TKey id, CancellationToken cancellationToken = default)
	{
		using var response = await HttpClientApi.DeleteAsync($"{RequestUriApi}/{id}", cancellationToken).ConfigureAwait(false);
		var content = await IApiService.ReadContentSafeAsync(response, cancellationToken).ConfigureAwait(false);

		if (response.StatusCode == HttpStatusCode.BadRequest)
			return ApiResult.Fail(IApiService.ExtractErrorMessage(content), (int)response.StatusCode);

		if (!response.IsSuccessStatusCode)
			return ApiResult.Fail(IApiService.ExtractErrorMessage(content), (int)response.StatusCode);

		return ApiResult.Ok((int)response.StatusCode);
	}

	public async Task<IApiResult<TDto>> ReadAsync(TKey id, CancellationToken cancellationToken = default)
	{
		using var response = await HttpClientApi.GetAsync($"{RequestUriApi}/{id}", cancellationToken).ConfigureAwait(false);
		var content = await IApiService.ReadContentSafeAsync(response, cancellationToken).ConfigureAwait(false);

		if (!response.IsSuccessStatusCode)
			return ApiResult<TDto>.Fail(IApiService.ExtractErrorMessage(content), (int)response.StatusCode);

		var dto = default(TDto);
		try { dto = content is not null ? IApiService.Deserialize<TDto>(content) : null; } catch { /* ignore */ }
		return ApiResult<TDto>.Ok(dto, (int)response.StatusCode);
	}

	public async Task<IApiResult<PagedResponse<TDto>>> ReadsAsync(int page, int pageSize, CancellationToken cancellationToken = default)
	{
		using var requestMessage = new HttpRequestMessage(HttpMethod.Get, RequestUriApi);
		requestMessage.Headers.Add("x-page", $"{page}");
		requestMessage.Headers.Add("x-page-size", $"{pageSize}");

		using var response = await HttpClientApi.SendAsync(requestMessage, cancellationToken).ConfigureAwait(false);
		var content = await IApiService.ReadContentSafeAsync(response, cancellationToken).ConfigureAwait(false);

		if (!response.IsSuccessStatusCode)
			return ApiResult<PagedResponse<TDto>>.Fail(IApiService.ExtractErrorMessage(content), (int)response.StatusCode);

		var empty = new PagedResponse<TDto>
		{
			PageNumber = page,
			PageSize = pageSize,
			TotalCount = 0,
			Items = [],
		};
		var dto = default(PagedResponse<TDto>);
		try { dto = content is not null ? IApiService.Deserialize<PagedResponse<TDto>>(content) : null; } catch { /* ignore */ }
		return ApiResult<PagedResponse<TDto>>.Ok(dto ?? empty, (int)response.StatusCode);
	}

	public async Task<IApiResult<bool>> UpdateAsync(TKey id, TDto data, CancellationToken cancellationToken = default)
	{
		if (data is null) return ApiResult<bool>.Fail("Data is null", 400);

		using var response = await HttpClientApi.PutAsJsonAsync($"{RequestUriApi}/{id}", data, cancellationToken).ConfigureAwait(false);
		var content = await IApiService.ReadContentSafeAsync(response, cancellationToken).ConfigureAwait(false);

		if (!response.IsSuccessStatusCode)
			return ApiResult<bool>.Fail(IApiService.ExtractErrorMessage(content), (int)response.StatusCode);

		return ApiResult<bool>.Ok(true, (int)response.StatusCode);
	}

}
