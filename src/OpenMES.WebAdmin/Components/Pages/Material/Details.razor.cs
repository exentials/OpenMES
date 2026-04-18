using OpenMES.Data.Dtos;
using OpenMES.WebAdmin.Components.Common;
using OpenMES.WebApiClient;

namespace OpenMES.WebAdmin.Components.Pages.Material
{
	partial class Details(MesClient mesClient) : BaseDetails<MaterialDto, int>("material", mesClient.Material)
	{
	}
}
