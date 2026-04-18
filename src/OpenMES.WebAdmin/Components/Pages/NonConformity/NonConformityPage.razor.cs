using OpenMES.Data.Dtos;
using OpenMES.WebAdmin.Components.Common;
using OpenMES.WebApiClient;

namespace OpenMES.WebAdmin.Components.Pages.NonConformity;

partial class NonConformityPage(MesClient mesClient)
	: BasePage<NonConformityDto, int, Edit>("nonconformity", mesClient.NonConformity)
{
}
