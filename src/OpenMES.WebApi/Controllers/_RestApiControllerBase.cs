using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenMES.Data.Common;
using OpenMES.Data.Contexts;
using OpenMES.Data.Dtos;
using OpenMES.Data.Entities.Interfaces;
using OpenMES.Data.Extensions;
using OpenMES.Data.Interfaces;

namespace OpenMES.WebApi.Controllers;

public abstract class RestApiControllerBase<TEntity, TDto, TKey>(OpenMESDbContext dbContext, ILogger logger) : ApiControllerBase(dbContext, logger)
	where TEntity : class, IDtoAdapter<TEntity, TDto>, IKey<TKey>
	where TDto : class, IKey<TKey>
{
	private DbSet<TEntity> DbSetEntity => DbContext.Set<TEntity>();

	/// <summary>
	/// Gets the base queryable collection used by this controller.
	/// </summary>
	/// <remarks>
	/// Override this to apply common includes/filters shared by read and tracking operations.
	/// </remarks>
	protected virtual IQueryable<TEntity> Query => DbSetEntity;

	/// <summary>
	/// Gets the queryable collection used for read-only operations.
	/// </summary>
	protected virtual IQueryable<TEntity> ReadQuery => Query.AsNoTracking();

	/// <summary>
	/// Gets the queryable collection used for tracking operations.
	/// </summary>
	protected virtual IQueryable<TEntity> TrackingQuery => Query;

	protected static async Task<PagedResponse<T>> PaginateAsync<T>(IQueryable<T> query, int page, int pageSize, CancellationToken cancellationToken)
	{
		var totalCount = await query.CountAsync(cancellationToken);
		var items = await query.Skip(page * pageSize).Take(pageSize).ToListAsync(cancellationToken);
		var pagedResponse = new PagedResponse<T>
		{
			PageNumber = page,
			PageSize = pageSize,
			TotalCount = totalCount,
			Items = items
		};
		return pagedResponse;
	}

	protected static TDto AsDto(TEntity entity)
	{
		return TEntity.AsDto(entity);
	}

	protected static TEntity AsEntity(TDto dto)
	{
		return TEntity.AsEntity(dto);
	}

	protected virtual async Task<TKey> CreateAsync(TDto data, CancellationToken cancellationToken)
	{
		await DbSetEntity.AddAsync(TEntity.AsEntity(data), cancellationToken);
		await DbContext.SaveChangesAsync(cancellationToken);
		return data.Id;
	}

	protected virtual IQueryable<TDto> ReadsAsync(params string[] orderBy)
	{
		IQueryable<TEntity> query = ReadQuery;
		if (orderBy.Length == 0)
		{
			query = query.OrderBy(t => t.Id);
		}
		else
		{
			orderBy.ToList().ForEach(o =>
			{
				if (o.StartsWith('-'))
				{
					query = query.OrderByPropertyDescending(o[1..]);
				}
				else
				{
					query = query.OrderByProperty(o);
				}
			});
		}
		return query.Select(x => AsDto(x));
	}

	protected virtual async Task<TDto?> ReadAsync(TKey id, CancellationToken cancellationToken = default)
	{
		var model = await ReadQuery.Where(t => Equals(t.Id, id)).FirstOrDefaultAsync(cancellationToken);
		return model is null ? default : AsDto(model);
	}

	protected async Task<bool> UpdateAsync(TKey id, TDto data, CancellationToken cancellationToken)
	{
		try
		{
			var model = await DbSetEntity.FindAsync([id], cancellationToken);
			if (model is null)
			{
				return false;
			}

			DbContext.Entry(model).CurrentValues.SetValues(AsEntity(data));

			if (model is IBaseDates x)
			{
				x.UpdatedAt = DateTimeOffset.UtcNow;
			}
			await DbContext.SaveChangesAsync(cancellationToken);
		}
		catch (DbUpdateConcurrencyException)
		{
			if (await DbSetEntity.FindAsync([id], cancellationToken) is null)
			{
				return false;
			}
			else
			{
				throw;
			}
		}
		return true;
	}

	protected virtual async Task<bool> DeleteAsync(TKey id, CancellationToken cancellationToken)
	{
		var model = await DbSetEntity.FindAsync([id], cancellationToken);
		if (model is null)
		{
			return false;
		}
		DbSetEntity.Remove(model);
		await DbContext.SaveChangesAsync(cancellationToken);
		return true;
	}

	[HttpPost]
	public async Task<ActionResult<TKey>> Create(TDto data, CancellationToken cancellationToken)
	{
		return await CreateAsync(data, cancellationToken);
	}

	[HttpGet]
	[ProducesResponseType<PagedResponse<PlantDto>>(StatusCodes.Status200OK)]
	public async Task<PagedResponse<TDto>> Reads([FromHeader(Name = "x-page")] int page, [FromHeader(Name = "x-page-size")] int pageSize, CancellationToken cancellationToken)
	{
		IQueryable<TDto> query = ReadsAsync();
		return await PaginateAsync(query, page, pageSize, cancellationToken);
	}

	[HttpGet("{id}")]
	public async Task<ActionResult<TDto>> Read(TKey id, CancellationToken cancellationToken)
	{
		var model = await ReadAsync(id, cancellationToken);
		if (model == null)
		{
			return NotFound();
		}
		return model;
	}

	[HttpPut("{id}")]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	public virtual async Task<IActionResult> Update(TKey id, TDto data, CancellationToken cancellationToken)
	{
		if (!Equals(id, data.Id))
		{
			return BadRequest();
		}
		try
		{
			if (!await UpdateAsync(id, data, cancellationToken))
			{
				return NotFound();
			}
		}
		catch (DbUpdateConcurrencyException ex)
		{
			throw new ProblemException("Concurrency error", ex.InnerException?.Message ?? ex.Message);
		}
		return NoContent();
	}

	[HttpDelete("{id}")]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	public virtual async Task<IActionResult> Delete(TKey id, CancellationToken cancellationToken)
	{
		try
		{
			if (!await DeleteAsync(id, cancellationToken))
			{
				return NotFound();
			}
		}
		catch (Exception ex)
		{
			throw new ProblemException("Delete item failed", ex.InnerException?.Message ?? ex.Message);
		}
		return NoContent();
	}

}
