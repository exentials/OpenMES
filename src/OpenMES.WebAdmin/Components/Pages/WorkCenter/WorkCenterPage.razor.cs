using OpenMES.Data.Dtos;
using OpenMES.WebAdmin.Components.Common;
using OpenMES.WebApiClient;

namespace OpenMES.WebAdmin.Components.Pages.WorkCenter;

partial class WorkCenterPage(MesClient mesClient)
	: BasePage<WorkCenterDto, int, Edit>("workcenter", mesClient.WorkCenter)
{
}
