using Microsoft.EntityFrameworkCore;
using OpenMES.Data.Dtos;
using OpenMES.Data.Interfaces;

namespace OpenMES.Data.Extensions;

public static class QueryExtensions
{
	extension<TEntity>(IQueryable<TEntity> query) where TEntity : class
	{
		public IOrderedQueryable<TEntity> OrderByProperty(string propertyName)
		{
			return query.OrderBy(e => EF.Property<object>(e, propertyName));
		}

		public IOrderedQueryable<TEntity> OrderByPropertyDescending(string propertyName)
		{
			return query.OrderByDescending(e => EF.Property<object>(e, propertyName));
		}
	}

	extension<TEntity>(IOrderedQueryable<TEntity> query) where TEntity : class
	{
		public IOrderedQueryable<TEntity> OrderByProperty(string propertyName)
		{
			return query.ThenBy(e => EF.Property<object>(e, propertyName));
		}

		public IOrderedQueryable<TEntity> OrderByPropertyDescending(string propertyName)
		{
			return query.ThenByDescending(e => EF.Property<object>(e, propertyName));
		}
	}

	//extension<TEntity>(TEntity) where TEntity : class, IKeyValueDtoAdapter<TEntity>
	//{
	//	public static KeyValueDto AsKeyValueDto(TEntity entity)
	//	{
	//		return TEntity.AsKeyValueDto(entity);
	//	}
	//}
}
