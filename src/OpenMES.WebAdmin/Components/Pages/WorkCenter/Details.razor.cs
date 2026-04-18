using OpenMES.Data.Dtos;
using OpenMES.WebAdmin.Components.Common;
using OpenMES.WebApiClient;

namespace OpenMES.WebAdmin.Components.Pages.WorkCenter;

partial class Details(MesClient mesClient)
	: BaseDetails<WorkCenterDto, int>("workcenter", mesClient.WorkCenter)
{
}
