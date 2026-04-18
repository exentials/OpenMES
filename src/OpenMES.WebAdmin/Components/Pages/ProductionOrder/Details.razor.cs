using OpenMES.Data.Dtos;
using OpenMES.WebAdmin.Components.Common;
using OpenMES.WebApiClient;

namespace OpenMES.WebAdmin.Components.Pages.ProductionOrder;

partial class Details(MesClient mesClient)
	: BaseDetails<ProductionOrderDto, int>("productionorder", mesClient.ProductionOrder)
{
}
