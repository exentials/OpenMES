using Microsoft.EntityFrameworkCore;
using OpenMES.Data.Contexts;
using OpenMES.Data.Dtos;
using OpenMES.Data.Entities;

namespace OpenMES.WebApi.Controllers;

public class WorkCenterController(OpenMESDbContext dbContext, ILogger<WorkCenterController> logger)
	: RestKeyValueApiControllerBase<WorkCenter, WorkCenterDto, int>(dbContext, logger)
{
	/// <summary>
	/// Gets the queryable collection of work centers, including related plant data.
	/// </summary>
	/// <remarks>The returned query includes the associated plant for each work center, enabling eager loading of
	/// plant information in queries. This property is typically used to build LINQ queries that require access to both
	/// work center and plant data.</remarks>
	protected override IQueryable<WorkCenter> Query => base.Query.Include(q => q.Plant);
}