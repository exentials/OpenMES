using OpenMES.Data.Dtos;
using OpenMES.WebAdmin.Components.Common;
using OpenMES.WebApiClient;

namespace OpenMES.WebAdmin.Components.Pages.StorageLocation;

partial class StorageLocationPage(MesClient mesClient)
	: BasePage<StorageLocationDto, int, Edit>("storagelocation", mesClient.StorageLocation)
{
}
