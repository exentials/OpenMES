using OpenMES.Data.Dtos;
using OpenMES.WebAdmin.Components.Common;
using OpenMES.WebApiClient;

namespace OpenMES.WebAdmin.Components.Pages.InspectionReading;

partial class InspectionReadingPage(MesClient mesClient)
	: BasePage<InspectionReadingDto, int, Edit>("inspectionreading", mesClient.InspectionReading)
{
}
