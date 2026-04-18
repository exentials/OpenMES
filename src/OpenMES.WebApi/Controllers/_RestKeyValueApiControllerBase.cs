using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenMES.Data.Common;
using OpenMES.Data.Contexts;
using OpenMES.Data.Dtos;
using OpenMES.Data.Entities.Interfaces;
using OpenMES.Data.Interfaces;

namespace OpenMES.WebApi.Controllers;

public abstract class RestKeyValueApiControllerBase<TEntity, TDto, TKey>(OpenMESDbContext dbContext, ILogger logger)
	: RestApiControllerBase<TEntity, TDto, TKey>(dbContext, logger)
	where TEntity : class, IDtoAdapter<TEntity, TDto>, IKey<TKey>, IKeyValueDtoAdapter<TEntity, TDto, TKey>
	where TDto : class, IKey<TKey>
{
	protected static KeyValueDto<TKey> AsKeyValueDto(TEntity entity) => TEntity.AsKeyValueDto(entity);

	[HttpGet("keyvalue")]
	[ProducesResponseType(StatusCodes.Status200OK)]
	public async Task<IEnumerable<KeyValueDto<TKey>>> ReadKeyValues([FromHeader(Name = "x-term")] string? term, [FromHeader(Name = "x-limit")] int? limit, CancellationToken cancellationToken)
	{
		var query = ReadQuery.Select(x => AsKeyValueDto(x));
		if (term is not null)
		{
			query = query.Where(x => x.Key.StartsWith(term) || x.Value.Contains(term));
		}
		//query = query.OrderBy(x => x.Key);
		if (limit.HasValue)
		{
			query = query.Take(limit.Value);
		}
		return await query.ToListAsync(cancellationToken);
	}
}
