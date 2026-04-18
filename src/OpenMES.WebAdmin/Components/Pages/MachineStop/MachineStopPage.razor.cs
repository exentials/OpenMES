using OpenMES.Data.Dtos;
using OpenMES.WebAdmin.Components.Common;
using OpenMES.WebApiClient;

namespace OpenMES.WebAdmin.Components.Pages.MachineStop;

partial class MachineStopPage(MesClient mesClient)
	: BasePage<MachineStopDto, int, Edit>("machinestop", mesClient.MachineStop)
{
}
