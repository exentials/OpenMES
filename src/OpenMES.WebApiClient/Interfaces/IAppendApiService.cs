using OpenMES.Data.Dtos;

namespace OpenMES.WebApiClient.Interfaces;

/// <summary>
/// Read-only + create service for append-only entities (e.g. StockMovement).
/// Update and Delete are intentionally not supported.
/// </summary>
public interface IAppendApiService<TDto, TKey> : IApiService where TDto : class
{
	Task<IApiResult<TDto>> ReadAsync(TKey id, CancellationToken cancellationToken = default);
	Task<IApiResult<PagedResponse<TDto>>> ReadsAsync(int page, int pageSize, CancellationToken cancellationToken = default);
	Task<IApiResult<TDto>> CreateAsync(TDto data, CancellationToken cancellationToken = default);
}
