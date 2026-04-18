using OpenMES.Data.Common;
using OpenMES.Data.Dtos;
using OpenMES.WebAdmin.Components.Common;
using OpenMES.WebApiClient;

namespace OpenMES.WebAdmin.Components.Pages.MachineState;

partial class MachineStatePage(MesClient mesClient)
	: BasePage<MachineStateDto, int, Edit>("machinestate", mesClient.MachineState)
{
	protected static string GetStatusColor(MachineStatus status) => status switch
	{
		MachineStatus.Running     => "green",
		MachineStatus.Idle        => "gray",
		MachineStatus.Setup       => "orange",
		MachineStatus.Stopped     => "red",
		MachineStatus.Maintenance => "purple",
		_                         => "black"
	};
}
