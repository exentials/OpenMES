using OpenMES.Data.Dtos;
using OpenMES.WebAdmin.Components.Common;
using OpenMES.WebApiClient;

namespace OpenMES.WebAdmin.Components.Pages.Operator;

partial class OperatorPage(MesClient mesClient)
	: BasePage<OperatorDto, int, Edit>("operator", mesClient.Operator)
{
}
