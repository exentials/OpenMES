namespace OpenMES.Data.Dtos;

public record KeyValueRequestDto
{
	public string? Term { get; init; }
	public int? Limit { get; init; }
	public Dictionary<string, object>? Filters { get; init; }
}