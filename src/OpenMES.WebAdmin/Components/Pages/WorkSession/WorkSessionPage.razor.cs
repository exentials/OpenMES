using OpenMES.Data.Dtos;
using OpenMES.WebAdmin.Components.Common;
using OpenMES.WebApiClient;

namespace OpenMES.WebAdmin.Components.Pages.WorkSession;

partial class WorkSessionPage(MesClient mesClient)
	: BasePage<WorkSessionDto, int, Edit>("worksession", mesClient.WorkSession)
{
}
