using OpenMES.Data.Dtos;
using OpenMES.WebAdmin.Components.Common;
using OpenMES.WebApiClient;

namespace OpenMES.WebAdmin.Components.Pages.ProductionDeclaration;

partial class ProductionDeclarationPage(MesClient mesClient)
	: BaseAppendPage<ProductionDeclarationDto, Edit>(mesClient.ProductionDeclaration)
{
}
