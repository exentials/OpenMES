using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using OpenMES.Data.Common;
using OpenMES.Data.Dtos;
using OpenMES.Data.Dtos.Resources;
using OpenMES.Localization.Resources;
using OpenMES.WebApiClient;
using OpenMES.WebClient.ViewModels;

namespace OpenMES.WebClient.Components.Pages;

partial class Action(
    ActionViewModel model,
    ILogger<Action> logger,
    IMessageService messageService,
    MesClient mesClient)
    : ClientComponentBase<ActionViewModel>(model, logger, messageService, mesClient)
{
    [Parameter] public int Id { get; set; }

    private bool IsOperatorDialogOpen { get; set; }
    private bool IsStopReasonDialogOpen { get; set; }

    private List<OperatorDto> AvailableOperators { get; set; } = [];
    private List<MachineStopReasonDto> AvailableStopReasons { get; set; } = [];
    private List<ProductionOrderPhaseDto> AvailablePhases { get; set; } = [];

    private int? SelectedOperatorId { get; set; }

    // ── Lifecycle ─────────────────────────────────────────────────────────
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        if (firstRender && CurrentIdentity?.IsAuthenticated == true)
            await LoadContextAsync();
    }

    private async Task LoadContextAsync()
    {
        await RunAsync(async ct =>
        {
            // Load machine, state, sessions and operator shift in parallel
            var machineTask  = MesClient.Machine.ReadAsync(Id, ct);
            var stateTask    = MesClient.MachineState.GetAllCurrentAsync(ct);
            var sessionsTask = MesClient.WorkSession.GetOpenAsync(ct);

            await Task.WhenAll(machineTask, stateTask, sessionsTask);

            ViewModel.Machine      = machineTask.Result.Data;
            ViewModel.CurrentState = stateTask.Result.FirstOrDefault(s => s.MachineId == Id);
            ViewModel.OpenSession  = sessionsTask.Result.FirstOrDefault(s => s.MachineId == Id);

            // If there is an open session, load the active phase
            if (ViewModel.OpenSession is not null)
            {
                SelectedOperatorId = ViewModel.OpenSession.OperatorId;

                var phaseResult = await MesClient.ProductionOrderPhase
                    .ReadAsync(ViewModel.OpenSession.ProductionOrderPhaseId, ct);
                ViewModel.ActivePhase = phaseResult.Data;
            }

            // Load latest shift event for the selected operator (drives check-in/out button label)
            if (SelectedOperatorId.HasValue)
            {
                var shiftResult = await MesClient.OperatorShift
                    .GetCurrentStatusAsync(SelectedOperatorId.Value, ct);
                ViewModel.OperatorShift = shiftResult;
            }
        });
    }

    private async Task<bool> EnsureOperatorSelectedAsync(
        CancellationToken ct,
        bool forceSelection = false,
        bool onlyPresent = false)
    {
        AvailableOperators = onlyPresent
            ? await GetPresentOperatorsAsync(ct)
            : await GetAllOperatorsAsync(ct);

        if (AvailableOperators.Count == 0)
        {
            ErrorService.AddError(UiResources.Action_ErrorNoOperators);
            return false;
        }

        if (!forceSelection &&
            SelectedOperatorId.HasValue &&
            AvailableOperators.Any(x => x.Id == SelectedOperatorId.Value))
        {
            return true;
        }

        IsOperatorDialogOpen = true;
        StateHasChanged();
        return false;
    }

    private async Task<List<OperatorDto>> GetAllOperatorsAsync(CancellationToken ct)
    {
        var operatorsResult = await MesClient.Operator.ReadsAsync(0, 500, ct);
        return operatorsResult.Data?.Items?.ToList() ?? [];
    }

    private async Task<List<OperatorDto>> GetPresentOperatorsAsync(CancellationToken ct)
    {
        var present = await MesClient.OperatorShift.GetPresentAsync(ct: ct);
        return present.ToList();
    }

    private async Task<bool> EnsureActivePhaseAsync(CancellationToken ct)
    {
        if (ViewModel.ActivePhase is not null)
        {
            return true;
        }

        if (ViewModel.Machine is null)
        {
            return false;
        }

        var phasesResult = await MesClient.ProductionOrderPhase.ReadsAsync(0, 200, ct);
        AvailablePhases = phasesResult.Data?.Items?
            .Where(p => p.WorkCenterId == ViewModel.Machine.WorkCenterId)
            .Where(p => p.Status is OrderStatus.Released or OrderStatus.Setup or OrderStatus.InProcess)
            .OrderBy(p => p.PhaseNumber)
            .ToList() ?? [];

        if (AvailablePhases.Count == 0)
        {
            ErrorService.AddError(UiResources.Action_ErrorNoActivePhaseForMachine);
            return false;
        }

        if (AvailablePhases.Count == 1)
        {
            ViewModel.ActivePhase = AvailablePhases[0];
            return true;
        }

        ViewModel.Screen = ActionScreen.PhasePicker;
        ViewModel.Notify();
        return false;
    }

    private async Task<bool> EnsureWorkSessionAsync(CancellationToken ct)
    {
        if (ViewModel.HasOpenSession)
        {
            return true;
        }

        if (!await EnsureOperatorSelectedAsync(ct, forceSelection: true, onlyPresent: true))
        {
            return false;
        }

        if (!await EnsureActivePhaseAsync(ct))
        {
            return false;
        }

        ViewModel.Screen = ActionScreen.SessionTypePicker;
        ViewModel.Notify();
        return false;
    }

    // ── Action handlers ───────────────────────────────────────────────────

    /// <summary>
    /// Entry point for "Start work" / "Start setup".
    /// Ensures operator and phase are selected before showing SessionTypePicker.
    /// </summary>
    private async Task StartWorkAsync()
    {
        await RunAsync(async ct =>
        {
            if (!await EnsureOperatorSelectedAsync(ct, forceSelection: true, onlyPresent: true)) return;
            if (!await EnsureActivePhaseAsync(ct)) return;

            // Phase resolved — show session type picker
            ViewModel.Screen = ActionScreen.SessionTypePicker;
            ViewModel.Notify();
        });
    }

    private async Task StartSessionOfType(WorkSessionType type)
    {
        await RunAsync(async ct =>
        {
            // Operator and phase must already be resolved at this point
            // (StartWorkAsync guarantees it). Store type in case phase
            // picker is re-entered from here as a safeguard.
            ViewModel.PendingSessionType = type;

            if (!await EnsureOperatorSelectedAsync(ct, forceSelection: false, onlyPresent: true)) return;
            if (!await EnsureActivePhaseAsync(ct)) return;

            await OpenSessionAsync(type, ct);
        });
    }

    /// <summary>Creates the work session and transitions back to Main.</summary>
    private async Task OpenSessionAsync(WorkSessionType type, CancellationToken ct)
    {
        var session = new WorkSessionDto
        {
            MachineId              = Id,
            ProductionOrderPhaseId = ViewModel.ActivePhase!.Id,
            OperatorId             = SelectedOperatorId!.Value,
            SessionType            = type,
            StartTime              = DateTimeOffset.UtcNow,
            Source                 = "Terminal",
        };

        var result = await MesClient.WorkSession.OpenSessionAsync(session, ct);
        if (result is null)
        {
            ErrorService.AddError(UiResources.Action_ErrorOpenSessionFailed);
        }
        else
        {
            ViewModel.OpenSession = result;
        }

        ViewModel.PendingSessionType = null;
        ViewModel.Screen = ActionScreen.Main;
        ViewModel.Notify();
    }

    /// <summary>Closes the current open session.</summary>
    private async Task CloseSessionAsync()
    {
        if (ViewModel.OpenSession is null) return;
        await RunAsync(async ct =>
        {
            var result = await MesClient.WorkSession.CloseSessionAsync(ViewModel.OpenSession.Id, ct);
            if (result is not null)
            {
                ViewModel.OpenSession = null;
            }
            ViewModel.Notify();
        });
    }

    /// <summary>Opens the operator shift screen (check-in / check-out / break).</summary>
    private Task ShowShiftScreenAsync()
    {
        ViewModel.Screen = ActionScreen.CheckIn;
        ViewModel.Notify();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Called when OperatorShiftScreen reports a shift change.
    /// Reloads context so the button label and presence state are up to date.
    /// </summary>
    private async Task OnShiftChangedAsync()
    {
        ViewModel.Screen = ActionScreen.Main;
        ViewModel.Notify();
        await LoadContextAsync();
    }

    /// <summary>Navigates to the declaration sub-screen.</summary>
    private async Task ShowDeclareAsync()
    {
        await RunAsync(async ct =>
        {
            if (!await EnsureWorkSessionAsync(ct))
            {
                return;
            }

            ViewModel.ConfirmedQty = 0;
            ViewModel.ScrapQty     = 0;
            ViewModel.Screen       = ActionScreen.Declare;
            ViewModel.Notify();
        });
    }

    /// <summary>Submits a production declaration and returns to main screen.</summary>
    private async Task ConfirmDeclareAsync()
    {
        if (ViewModel.ActivePhase is null || ViewModel.ConfirmedQty <= 0) return;
        await RunAsync(async ct =>
        {
            var dto = new ProductionDeclarationDto
            {
                ProductionOrderPhaseId = ViewModel.ActivePhase.Id,
                MachineId              = Id,
                OperatorId             = SelectedOperatorId ?? ViewModel.OpenSession?.OperatorId ?? 0,
                ConfirmedQuantity      = ViewModel.ConfirmedQty,
                ScrapQuantity          = ViewModel.ScrapQty,
                DeclarationDate        = DateTimeOffset.UtcNow,
            };
            await MesClient.ProductionDeclaration.CreateAsync(dto, ct);
            ViewModel.Screen = ActionScreen.Main;
            ViewModel.Notify();
        });
        await LoadContextAsync();
    }

    private async Task HandleMachineStateAsync()
    {
        if (ViewModel.Status == MachineStatus.Running)
        {
            await RunAsync(async ct =>
            {
                if (!await EnsureOperatorSelectedAsync(ct, onlyPresent: true))
                {
                    return;
                }

                var reasons = await MesClient.MachineStopReason.ReadsAsync(0, 200, ct);
                AvailableStopReasons = reasons.Data?.Items?.Where(r => !r.Disabled).ToList() ?? [];
                if (AvailableStopReasons.Count == 0)
                {
                    ErrorService.AddError(UiResources.Action_ErrorNoStopReasons);
                    return;
                }

                IsStopReasonDialogOpen = true;
                StateHasChanged();
            });
            return;
        }

        await RunAsync(async ct =>
        {
            if (!await EnsureOperatorSelectedAsync(ct, onlyPresent: true))
            {
                return;
            }

            var state = new MachineStateDto
            {
                MachineId = Id,
                Status = MachineStatus.Running,
                EventTime = DateTimeOffset.UtcNow,
                Source = "Terminal",
                OperatorId = SelectedOperatorId,
            };

            var result = await MesClient.MachineState.CreateAsync(state, ct);
            if (result.Success)
            {
                ViewModel.CurrentState = result.Data;
                ViewModel.Notify();
            }
        });
    }

    private async Task OnOperatorSelectedAsync(int operatorId)
    {
        IsOperatorDialogOpen = false;
        SelectedOperatorId = operatorId;
        await Task.CompletedTask;
    }

    private Task CloseOperatorDialog()
    {
        IsOperatorDialogOpen = false;
        return Task.CompletedTask;
    }

    private async Task OnStopReasonSelectedAsync(int stopReasonId)
    {
        IsStopReasonDialogOpen = false;

        await RunAsync(async ct =>
        {
            if (!await EnsureOperatorSelectedAsync(ct, onlyPresent: true))
            {
                return;
            }

            var now = DateTimeOffset.UtcNow;

            var state = new MachineStateDto
            {
                MachineId = Id,
                Status = MachineStatus.Stopped,
                EventTime = now,
                Source = "Terminal",
                OperatorId = SelectedOperatorId,
            };

            var stateResult = await MesClient.MachineState.CreateAsync(state, ct);
            if (stateResult.Success)
            {
                ViewModel.CurrentState = stateResult.Data;
            }

            var machineStop = new MachineStopDto
            {
                MachineId = Id,
                ProductionOrderPhaseId = ViewModel.ActivePhase?.Id,
                MachineStopReasonId = stopReasonId,
                StartDate = now,
                EndDate = null,
                Notes = null,
            };

            await MesClient.MachineStop.CreateAsync(machineStop, ct);
            ViewModel.Notify();
        });
    }

    private Task CloseStopReasonDialog()
    {
        IsStopReasonDialogOpen = false;
        return Task.CompletedTask;
    }

    private async Task OnPhaseSelectedAsync(ProductionOrderPhaseDto phase)
    {
        ViewModel.ActivePhase = phase;

        // If a session type was pending (user came from SessionTypePicker →
        // PhasePicker), complete the open-session flow immediately.
        if (ViewModel.PendingSessionType.HasValue)
        {
            var type = ViewModel.PendingSessionType.Value;
            await RunAsync(ct => OpenSessionAsync(type, ct));
            return;
        }

        ViewModel.Screen = ActionScreen.Main;
        ViewModel.Notify();
    }

    private void GoBack()
    {
        if (ViewModel.Screen == ActionScreen.PhasePicker)
            ViewModel.ActivePhase = null;  // allow re-selection next time
        ViewModel.Screen = ActionScreen.Main;
        ViewModel.Notify();
    }

    private void NavigateHome() => Navigation.NavigateTo("/");
}
