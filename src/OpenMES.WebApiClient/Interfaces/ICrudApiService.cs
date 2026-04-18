using OpenMES.Data.Dtos;

namespace OpenMES.WebApiClient.Interfaces;

public interface ICrudApiService<TDto, TKey> : IApiService where TDto : class
{
	Task<IApiResult<TDto>> ReadAsync(TKey id, CancellationToken cancellationToken = default);
	Task<IApiResult<PagedResponse<TDto>>> ReadsAsync(int page, int pageSize, CancellationToken cancellationToken = default);
	Task<IApiResult<TDto>> CreateAsync(TDto data, CancellationToken cancellationToken = default);
	Task<IApiResult<bool>> UpdateAsync(TKey id, TDto data, CancellationToken cancellationToken = default);
	Task<IApiResult> DeleteAsync(TKey id, CancellationToken cancellationToken = default);
}

