using OpenMES.Data.Dtos;
using OpenMES.WebAdmin.Components.Common;
using OpenMES.WebApiClient;

namespace OpenMES.WebAdmin.Components.Pages.InspectionPlan;

partial class Details(MesClient mesClient)
	: BaseDetails<InspectionPlanDto, int>("inspectionplan", mesClient.InspectionPlan)
{
}
