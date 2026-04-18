using OpenMES.Data.Dtos;
using OpenMES.WebAdmin.Components.Common;
using OpenMES.WebApiClient;

namespace OpenMES.WebAdmin.Components.Pages.StockMovement;

partial class StockMovementPage(MesClient mesClient)
	: BaseAppendPage<StockMovementDto, Edit>(mesClient.StockMovement)
{
}
