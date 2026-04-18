using OpenMES.Data.Dtos;
using OpenMES.WebAdmin.Components.Common;
using OpenMES.WebApiClient;

namespace OpenMES.WebAdmin.Components.Pages.ProductionOrder;

partial class ProductionOrderPage(MesClient mesClient)
	: BasePage<ProductionOrderDto, int, Edit>("productionorder", mesClient.ProductionOrder)
{
}
