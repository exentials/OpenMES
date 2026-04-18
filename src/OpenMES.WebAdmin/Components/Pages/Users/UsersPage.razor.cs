using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using OpenMES.Data.Dtos;
using OpenMES.WebApiClient;

namespace OpenMES.WebAdmin.Components.Pages.Users;

[Authorize(Roles = "admin")]
public class UsersPageBase : ComponentBase
{
    [Inject] MesClient MesClient { get; set; } = null!;
    [Inject] IDialogService DialogService { get; set; } = null!;
    [Inject] IToastService ToastService { get; set; } = null!;

    protected List<IdentityRoleDto> _availableRoles = [];
    protected string? _errorMessage;
    protected readonly PaginationState pagination = new() { ItemsPerPage = 15 };

    protected override async Task OnInitializedAsync()
    {
        await LoadRolesAsync();
    }

    private async Task LoadRolesAsync()
    {
        _errorMessage = null;
        try
        {
            _availableRoles = await MesClient.Users.GetRolesAsync();
        }
        catch (Exception ex)
        {
            _errorMessage = $"Errore caricamento ruoli: {ex.Message}";
        }
    }

    protected async ValueTask<GridItemsProviderResult<IdentityUserDto>> ItemsProvider(
        GridItemsProviderRequest<IdentityUserDto> request)
    {
        try
        {
            var page   = request.StartIndex / pagination.ItemsPerPage;
            var paged  = await MesClient.Users.GetUsersAsync(page, pagination.ItemsPerPage);

            return GridItemsProviderResult.From<IdentityUserDto>([.. paged.Items], paged.TotalCount);
        }
        catch (Exception ex)
        {
            _errorMessage = ex.Message;
            return GridItemsProviderResult.From<IdentityUserDto>([], 0);
        }
    }

    protected async Task RefreshAsync()
    {
        await pagination.SetTotalItemCountAsync(pagination.TotalItemCount ?? 0);
        await InvokeAsync(StateHasChanged);
    }

    protected async Task AddInDialog()
    {
        var dialog = await DialogService.ShowDialogAsync<UserEditDialog>(
            new UserDialogModel { IsNew = true, AvailableRoles = _availableRoles },
            new DialogParameters { Title = "Add", PreventDismissOnOverlayClick = true, PreventScroll = true });

        var result = await dialog.Result;
        if (result.Cancelled || result.Data is not UserDialogModel data) return;

        try
        {
            await MesClient.Users.CreateUserAsync(new CreateIdentityUserDto
            {
                Email    = data.Email,
                Password = data.Password,
                Roles    = [.. data.SelectedRoles],
            });
            ToastService.ShowSuccess($"User {data.Email} created.");
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            _errorMessage = ex.Message;
        }
    }

    protected async Task EditInDialog(IdentityUserDto user)
    {
        var dialog = await DialogService.ShowDialogAsync<UserEditDialog>(
            new UserDialogModel
            {
                IsNew          = false,
                UserId         = user.Id,
                Email          = user.Email,
                SelectedRoles  = [.. user.Roles],
                AvailableRoles = _availableRoles,
            },
            new DialogParameters { Title = "Edit", PreventDismissOnOverlayClick = true, PreventScroll = true });

        var result = await dialog.Result;
        if (result.Cancelled || result.Data is not UserDialogModel data) return;

        try
        {
            await MesClient.Users.UpdateUserAsync(user.Id, new UpdateIdentityUserDto
            {
                NewPassword = string.IsNullOrWhiteSpace(data.Password) ? null : data.Password,
                Roles       = [.. data.SelectedRoles],
            });
            ToastService.ShowSuccess("User updated.");
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            _errorMessage = ex.Message;
        }
    }

    protected async Task UnlockUserAsync(IdentityUserDto user)
    {
        try
        {
            await MesClient.Users.UnlockUserAsync(user.Id);
            ToastService.ShowSuccess($"Account {user.Email} sbloccato.");
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            _errorMessage = ex.Message;
        }
    }

    protected async Task DeleteUserAsync(IdentityUserDto user)
    {
        var confirm = await DialogService.ShowConfirmationAsync(
            $"Eliminare l'utente {user.Email}? L'operazione è irreversibile.",
            "Sì, elimina", "Annulla", "Conferma eliminazione");
        var result = await confirm.Result;
        if (result.Cancelled) return;

        try
        {
            await MesClient.Users.DeleteUserAsync(user.Id);
            ToastService.ShowSuccess($"Utente {user.Email} eliminato.");
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            _errorMessage = ex.Message;
        }
    }
}

/// <summary>Modello condiviso tra la pagina e il dialog di modifica.</summary>
public class UserDialogModel
{
    public bool   IsNew          { get; set; }
    public string UserId         { get; set; } = string.Empty;
    public string Email          { get; set; } = string.Empty;
    public string Password       { get; set; } = string.Empty;
    public List<string> SelectedRoles  { get; set; } = [];
    public List<IdentityRoleDto> AvailableRoles { get; set; } = [];
}
