using OpenMES.Data.Dtos;
using OpenMES.WebAdmin.Components.Common;
using OpenMES.WebApiClient;

namespace OpenMES.WebAdmin.Components.Pages.Machine;

partial class MachinePage(MesClient mesClient)
	: BasePage<MachineDto, int, Edit>("machine", mesClient.Machine)
{
}
