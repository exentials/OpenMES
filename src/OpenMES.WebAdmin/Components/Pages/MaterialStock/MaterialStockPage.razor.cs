using OpenMES.Data.Dtos;
using OpenMES.WebAdmin.Components.Common;
using OpenMES.WebApiClient;

namespace OpenMES.WebAdmin.Components.Pages.MaterialStock;

partial class MaterialStockPage(MesClient mesClient)
	: BasePage<MaterialStockDto, int, Edit>("materialstock", mesClient.MaterialStock)
{
}
