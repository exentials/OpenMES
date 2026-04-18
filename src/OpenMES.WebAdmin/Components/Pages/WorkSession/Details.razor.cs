using OpenMES.Data.Dtos;
using OpenMES.WebAdmin.Components.Common;
using OpenMES.WebApiClient;

namespace OpenMES.WebAdmin.Components.Pages.WorkSession;

partial class Details(MesClient mesClient)
	: BaseDetails<WorkSessionDto, int>("worksession", mesClient.WorkSession)
{
}
