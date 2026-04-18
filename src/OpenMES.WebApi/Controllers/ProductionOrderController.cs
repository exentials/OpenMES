using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using OpenMES.Data.Contexts;
using OpenMES.Data.Dtos;
using OpenMES.Data.Entities;

namespace OpenMES.WebApi.Controllers;

/// <summary>Production orders with phases and denormalized material/plant data.</summary>
[Authorize(AuthenticationSchemes = "JWT,TerminalScheme", Roles = "Admin, User, device")]
public class ProductionOrderController(OpenMESDbContext dbContext, ILogger<ProductionOrderController> logger)
	: RestApiControllerBase<ProductionOrder, ProductionOrderDto, int>(dbContext, logger)
{
	protected override IQueryable<ProductionOrder> Query => base.Query
		.Include(x => x.Material)
		.Include(x => x.Plant)
		.Include(x => x.ProductionOrderPhases);
}
