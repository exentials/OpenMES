using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using OpenMES.Data.Contexts;
using OpenMES.Data.Dtos;
using OpenMES.Data.Entities;

namespace OpenMES.WebApi.Controllers;

/// <summary>Non-conformities raised during production or quality inspection.</summary>
[Authorize(Roles = "Admin, User")]
public class NonConformityController(OpenMESDbContext dbContext, ILogger<NonConformityController> logger)
	: RestApiControllerBase<NonConformity, NonConformityDto, int>(dbContext, logger)
{
	protected override IQueryable<NonConformity> Query => base.Query
		.Include(x => x.ProductionOrderPhase)
		.Include(x => x.ClosedByOperator);
}
