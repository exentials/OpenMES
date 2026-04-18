using OpenMES.Data.Dtos;
using OpenMES.WebAdmin.Components.Common;
using OpenMES.WebApiClient;

namespace OpenMES.WebAdmin.Components.Pages.MachineStopReason;

partial class MachineStopReasonPage(MesClient mesClient)
	: BasePage<MachineStopReasonDto, int, Edit>("machinestopreason", mesClient.MachineStopReason)
{
}
