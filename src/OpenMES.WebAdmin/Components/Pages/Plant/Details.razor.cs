using OpenMES.Data.Dtos;
using OpenMES.WebAdmin.Components.Common;
using OpenMES.WebApiClient;

namespace OpenMES.WebAdmin.Components.Pages.Plant
{
	partial class Details(MesClient mesClient) : BaseDetails<PlantDto, int>("plant", mesClient.Plant)
	{
	}
}
