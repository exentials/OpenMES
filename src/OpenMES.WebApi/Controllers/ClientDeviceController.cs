using Microsoft.AspNetCore.Authorization;
using OpenMES.Data.Contexts;
using OpenMES.Data.Dtos;
using OpenMES.Data.Entities;

namespace OpenMES.WebApi.Controllers;

/// <summary>Shop floor terminal devices (client machines).</summary>
[Authorize(Roles = "Admin, User")]
public class ClientDeviceController(OpenMESDbContext dbContext, ILogger<ClientDeviceController> logger)
	: RestApiControllerBase<ClientDevice, ClientDeviceDto, int>(dbContext, logger)
{
}
