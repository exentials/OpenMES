using OpenMES.Data.Common;

namespace OpenMES.Data.Dtos;

public class KeyValueDto<TKey>
{
	public required TKey Id { get; init; }
	public required string Key { get; init; }
	public required string Value { get; init; }
}
