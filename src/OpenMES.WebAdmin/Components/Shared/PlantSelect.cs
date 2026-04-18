using Microsoft.AspNetCore.Components;
using OpenMES.Data.Dtos;
using OpenMES.WebAdmin.Components.Common;
using OpenMES.WebApiClient;

namespace OpenMES.WebAdmin.Components.Shared;

public class PlantSelect : SelectComponent<PlantDto, int>
{
    [Inject] private MesClient MesClient { get; set; } = null!;

    protected override void OnInitialized()
    {
        ApiService = MesClient.Plant;
        base.OnInitialized();
    }
}
