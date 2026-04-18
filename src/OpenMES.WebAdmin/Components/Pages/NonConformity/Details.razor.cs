using OpenMES.Data.Dtos;
using OpenMES.WebAdmin.Components.Common;
using OpenMES.WebApiClient;

namespace OpenMES.WebAdmin.Components.Pages.NonConformity;

partial class Details(MesClient mesClient)
	: BaseDetails<NonConformityDto, int>("nonconformity", mesClient.NonConformity)
{
}
