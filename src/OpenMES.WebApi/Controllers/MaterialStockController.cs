using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using OpenMES.Data.Contexts;
using OpenMES.Data.Dtos;
using OpenMES.Data.Entities;

namespace OpenMES.WebApi.Controllers;

/// <summary>Current stock levels per material and storage location.</summary>
[Authorize(AuthenticationSchemes = "JWT,TerminalScheme", Roles = "Admin, User, device")]
public class MaterialStockController(OpenMESDbContext dbContext, ILogger<MaterialStockController> logger)
	: RestApiControllerBase<MaterialStock, MaterialStockDto, int>(dbContext, logger)
{
	protected override IQueryable<MaterialStock> Query => base.Query
		.Include(x => x.Material)
		.Include(x => x.StorageLocation);
}
