using OpenMES.Data.Dtos;
using OpenMES.WebAdmin.Components.Common;
using OpenMES.WebApiClient;

namespace OpenMES.WebAdmin.Components.Pages.InspectionPlan;

partial class InspectionPlanPage(MesClient mesClient)
	: BasePage<InspectionPlanDto, int, Edit>("inspectionplan", mesClient.InspectionPlan)
{
}
