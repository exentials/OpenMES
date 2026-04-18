namespace OpenMES.Data.Dtos;

public sealed class PagedResponse<T>()
{
	public int PageNumber { get; init; }
	public int PageSize { get; init; }
	public int TotalCount { get; init; }
	public required IEnumerable<T> Items { get; init; }

	public static PagedResponse<T> Empty(int page, int pageSize) => new()
	{
		PageNumber = page,
		PageSize = pageSize,
		TotalCount = 0,
		Items = []
	};
}
