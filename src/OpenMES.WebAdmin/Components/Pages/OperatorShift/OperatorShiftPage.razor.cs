using OpenMES.Data.Dtos;
using OpenMES.WebAdmin.Components.Common;
using OpenMES.WebApiClient;

namespace OpenMES.WebAdmin.Components.Pages.OperatorShift;

partial class OperatorShiftPage(MesClient mesClient)
	: BasePage<OperatorShiftDto, int, Edit>("operatorshift", mesClient.OperatorShift)
{
}
