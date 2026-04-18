using OpenMES.Data.Contexts;
using OpenMES.Data.Dtos;
using OpenMES.Data.Entities;

namespace OpenMES.WebApi.Controllers;

public class PlantController(OpenMESDbContext dbContext, ILogger<PlantController> logger)
	: RestKeyValueApiControllerBase<Plant, PlantDto, int>(dbContext, logger)
{
}
