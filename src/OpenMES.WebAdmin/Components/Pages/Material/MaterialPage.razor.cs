using OpenMES.Data.Dtos;
using OpenMES.WebAdmin.Components.Common;
using OpenMES.WebApiClient;

namespace OpenMES.WebAdmin.Components.Pages.Material;

partial class MaterialPage(MesClient mesClient) : BasePage<MaterialDto, int, Edit>("material", mesClient.Material)
{
}

