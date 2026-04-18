using OpenMES.Data.Dtos;
using OpenMES.WebAdmin.Components.Common;
using OpenMES.WebApiClient;

namespace OpenMES.WebAdmin.Components.Pages.PhasePickingList;

partial class PhasePickingListPage(MesClient mesClient)
	: BasePage<PhasePickingListDto, int, Edit>("phasepickinglist", mesClient.PhasePickingList)
{
}
