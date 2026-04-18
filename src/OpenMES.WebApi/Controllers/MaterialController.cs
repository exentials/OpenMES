using OpenMES.Data.Dtos;
using OpenMES.Data.Entities;
using OpenMES.Data.Contexts;
using Microsoft.AspNetCore.Authorization;

namespace OpenMES.WebApi.Controllers;

[Authorize(AuthenticationSchemes = "JWT,TerminalScheme", Roles = "Admin, User, device")]
public class MaterialController(OpenMESDbContext dbContext, ILogger<MaterialController> logger)
	: RestApiControllerBase<Material, MaterialDto, int>(dbContext, logger)
{

}
