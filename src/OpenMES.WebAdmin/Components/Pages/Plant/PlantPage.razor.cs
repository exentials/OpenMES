using OpenMES.Data.Dtos;
using OpenMES.WebAdmin.Components.Common;
using OpenMES.WebApiClient;

namespace OpenMES.WebAdmin.Components.Pages.Plant;

partial class PlantPage(MesClient mesClient) : BasePage<PlantDto, int, Edit>("plant", mesClient.Plant)
{
}
