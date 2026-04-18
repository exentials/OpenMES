using Microsoft.AspNetCore.Authorization;
using OpenMES.Data.Contexts;
using OpenMES.Data.Dtos;
using OpenMES.Data.Entities;

namespace OpenMES.WebApi.Controllers;

/// <summary>Physical storage locations (warehouse / zone / slot).</summary>
[Authorize(AuthenticationSchemes = "JWT,TerminalScheme", Roles = "Admin, User, device")]
public class StorageLocationController(OpenMESDbContext dbContext, ILogger<StorageLocationController> logger)
	: RestApiControllerBase<StorageLocation, StorageLocationDto, int>(dbContext, logger)
{
	protected override IQueryable<StorageLocation> Query => base.Query
		.OrderBy(x => x.Warehouse)
		.ThenBy(x => x.Zone)
		.ThenBy(x => x.Slot);
}
