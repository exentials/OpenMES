using OpenMES.Data.Dtos;
using OpenMES.WebAdmin.Components.Common;
using OpenMES.WebApiClient;

namespace OpenMES.WebAdmin.Components.Pages.ClientDevice;

partial class ClientDevicePage(MesClient mesClient)
	: BasePage<ClientDeviceDto, int, Edit>("clientdevice", mesClient.ClientDevice)
{
}
