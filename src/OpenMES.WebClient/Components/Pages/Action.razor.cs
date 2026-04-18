using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using OpenMES.Data.Common;
using OpenMES.Data.Dtos;
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
    private bool ResumeStartWorkAfterOperatorSelection { get; set; }

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
            var machineTask = MesClient.Machine.ReadAsync(Id, ct);
            var stateTask = MesClient.MachineState.GetAllCurrentAsync(ct);
            var sessionsTask = MesClient.WorkSession.GetOpenAsync(ct);
            var placementsTask = MesClient.MachinePhasePlacement.GetOpenByMachineAsync(Id, ct);

            await Task.WhenAll(machineTask, stateTask, sessionsTask, placementsTask);

            ViewModel.Machine = machineTask.Result.Data;
            ViewModel.CurrentState = stateTask.Result.FirstOrDefault(s => s.MachineId == Id);
            ViewModel.OpenSession = sessionsTask.Result.FirstOrDefault(s => s.MachineId == Id);
            ViewModel.OpenPlacements = placementsTask.Result.ToList();

            if (ViewModel.OpenSession is not null)
            {
                SelectedOperatorId = ViewModel.OpenSession.OperatorId;
                var phaseResult = await MesClient.ProductionOrderPhase.ReadAsync(ViewModel.OpenSession.ProductionOrderPhaseId, ct);
                ViewModel.ActivePhase = phaseResult.Data;

                var placement = ViewModel.OpenPlacements
                    .FirstOrDefault(x => x.ProductionOrderPhaseId == ViewModel.OpenSession.ProductionOrderPhaseId);
                ViewModel.SelectedPlacementId = placement?.Id;
            }
            else if (ViewModel.OpenPlacements.Count > 0)
            {
                var placement = ViewModel.OpenPlacements.FirstOrDefault(x => x.Id == ViewModel.SelectedPlacementId)
                    ?? ViewModel.OpenPlacements[0];

                ViewModel.SelectedPlacementId = placement.Id;
                SelectedOperatorId = placement.PlacedByOperatorId;

                var phaseResult = await MesClient.ProductionOrderPhase.ReadAsync(placement.ProductionOrderPhaseId, ct);
                ViewModel.ActivePhase = phaseResult.Data;
            }
            else
            {
                ViewModel.SelectedPlacementId = null;
            }

            if (SelectedOperatorId.HasValue)
            {
                ViewModel.OperatorShift = await MesClient.OperatorShift.GetCurrentStatusAsync(SelectedOperatorId.Value, ct);
            }

            ViewModel.Notify();
        });
    }

    private async Task<bool> EnsureOperatorSelectedAsync(CancellationToken ct, bool forceSelection = false, bool onlyPresent = false)
    {
        AvailableOperators = onlyPresent
            ? (await MesClient.OperatorShift.GetPresentAsync(ct: ct)).ToList()
            : (await MesClient.Operator.ReadsAsync(0, 500, ct)).Data?.Items?.ToList() ?? [];

        if (AvailableOperators.Count == 0)
        {
            ErrorService.AddError(UiResources.Action_ErrorNoOperators);
            return false;
        }

        if (!forceSelection && SelectedOperatorId.HasValue && AvailableOperators.Any(x => x.Id == SelectedOperatorId.Value))
            return true;

        IsOperatorDialogOpen = true;
        StateHasChanged();
        return false;
    }

    private async Task<bool> EnsureActivePhaseAsync(CancellationToken ct)
    {
        if (ViewModel.ActivePhase is not null)
            return true;

        if (ViewModel.Machine is null)
            return false;

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

    private async Task<MachinePhasePlacementDto?> EnsurePlacementForActivePhaseAsync(CancellationToken ct)
    {
        if (ViewModel.ActivePhase is null)
            return null;

        var existing = ViewModel.OpenPlacements.FirstOrDefault(x => x.ProductionOrderPhaseId == ViewModel.ActivePhase.Id);
        if (existing is not null)
        {
            ViewModel.SelectedPlacementId = existing.Id;
            return existing;
        }

        if (!SelectedOperatorId.HasValue)
        {
            ErrorService.AddError(UiResources.Action_ErrorNoOperators);
            return null;
        }

        var created = await MesClient.MachinePhasePlacement.PlaceAsync(new MachinePhasePlacementDto
        {
            MachineId = Id,
            ProductionOrderPhaseId = ViewModel.ActivePhase.Id,
            PlacedByOperatorId = SelectedOperatorId.Value,
            PlacedAt = DateTimeOffset.UtcNow,
            Source = "Terminal",
        }, ct);

        if (created is null)
        {
            ErrorService.AddError(UiResources.Action_ErrorOpenSessionFailed);
            return null;
        }

        ViewModel.OpenPlacements.Add(created);
        ViewModel.SelectedPlacementId = created.Id;
        return created;
    }

    private async Task SelectPlacementAsync(MachinePhasePlacementDto placement, CancellationToken ct)
    {
        ViewModel.SelectedPlacementId = placement.Id;
        SelectedOperatorId = placement.PlacedByOperatorId;

        var phaseResult = await MesClient.ProductionOrderPhase.ReadAsync(placement.ProductionOrderPhaseId, ct);
        ViewModel.ActivePhase = phaseResult.Data;
    }

    // ── Action handlers ───────────────────────────────────────────────────

    private async Task StartWorkAsync()
    {
        await RunAsync(async ct =>
        {
            if (!await EnsureOperatorSelectedAsync(ct, forceSelection: true, onlyPresent: true))
            {
                ResumeStartWorkAfterOperatorSelection = IsOperatorDialogOpen;
                return;
            }

            if (!await EnsureActivePhaseAsync(ct))
                return;

            ResumeStartWorkAfterOperatorSelection = false;
            ViewModel.Screen = ActionScreen.SessionTypePicker;
            ViewModel.Notify();
        });
    }

    private async Task StartSessionOfType(WorkSessionType type)
    {
        await RunAsync(async ct =>
        {
            ViewModel.PendingSessionType = type;

            if (!await EnsureOperatorSelectedAsync(ct, onlyPresent: true)) return;
            if (!await EnsureActivePhaseAsync(ct)) return;

            var placement = await EnsurePlacementForActivePhaseAsync(ct);
            if (placement is null) return;

            MachinePhasePlacementDto? result = type switch
            {
                WorkSessionType.Setup when placement.Status == MachinePhasePlacementStatus.SetupPaused
                    => await MesClient.MachinePhasePlacement.ResumeSetupAsync(placement.Id, SelectedOperatorId, ct),
                WorkSessionType.Setup when placement.Status == MachinePhasePlacementStatus.Placed
                    => await MesClient.MachinePhasePlacement.StartSetupAsync(placement.Id, SelectedOperatorId, ct),
                WorkSessionType.Work when placement.Status == MachinePhasePlacementStatus.WorkPaused
                    => await MesClient.MachinePhasePlacement.ResumeWorkAsync(placement.Id, SelectedOperatorId, ct),
                WorkSessionType.Work when placement.Status is MachinePhasePlacementStatus.Placed or MachinePhasePlacementStatus.InSetup or MachinePhasePlacementStatus.SetupPaused
                    => await MesClient.MachinePhasePlacement.StartWorkAsync(placement.Id, SelectedOperatorId, ct),
                _ => null,
            };

            if (result is null)
            {
                ErrorService.AddError(UiResources.Action_ErrorOpenSessionFailed);
                return;
            }

            ViewModel.Screen = ActionScreen.Main;
            ViewModel.PendingSessionType = null;
            await LoadContextAsync();
        });
    }

    private async Task CloseSessionAsync()
    {
        await RunAsync(async ct =>
        {
            var placement = ViewModel.ActivePlacement;
            MachinePhasePlacementDto? result = null;

            if (placement is not null)
            {
                result = placement.Status switch
                {
                    MachinePhasePlacementStatus.InSetup => await MesClient.MachinePhasePlacement.PauseSetupAsync(placement.Id, ct),
                    MachinePhasePlacementStatus.InWork => await MesClient.MachinePhasePlacement.PauseWorkAsync(placement.Id, ct),
                    _ => null,
                };
            }

            if (result is null && ViewModel.OpenSession is not null)
            {
                var closed = await MesClient.WorkSession.CloseSessionAsync(ViewModel.OpenSession.Id, ct);
                if (closed is null)
                {
                    ErrorService.AddError(UiResources.Action_ErrorOpenSessionFailed);
                    return;
                }
            }

            await LoadContextAsync();
        });
    }

    private async Task OnPlacementSelectedAsync(MachinePhasePlacementDto placement)
    {
        await RunAsync(async ct =>
        {
            await SelectPlacementAsync(placement, ct);
            ViewModel.Notify();
        });
    }

    private async Task StartSetupForPlacementAsync(MachinePhasePlacementDto placement)
    {
        await RunAsync(async ct =>
        {
            await SelectPlacementAsync(placement, ct);
            await StartSessionOfType(WorkSessionType.Setup);
        });
    }

    private async Task StartWorkForPlacementAsync(MachinePhasePlacementDto placement)
    {
        await RunAsync(async ct =>
        {
            await SelectPlacementAsync(placement, ct);
            await StartSessionOfType(WorkSessionType.Work);
        });
    }

    private async Task PausePlacementAsync(MachinePhasePlacementDto placement)
    {
        await RunAsync(async ct =>
        {
            var result = placement.Status switch
            {
                MachinePhasePlacementStatus.InSetup => await MesClient.MachinePhasePlacement.PauseSetupAsync(placement.Id, ct),
                MachinePhasePlacementStatus.InWork => await MesClient.MachinePhasePlacement.PauseWorkAsync(placement.Id, ct),
                _ => null,
            };

            if (result is null)
            {
                ErrorService.AddError(UiResources.Action_ErrorOpenSessionFailed);
                return;
            }

            await LoadContextAsync();
        });
    }

    private async Task ResumePlacementAsync(MachinePhasePlacementDto placement)
    {
        await RunAsync(async ct =>
        {
            await SelectPlacementAsync(placement, ct);
            var result = placement.Status switch
            {
                MachinePhasePlacementStatus.SetupPaused => await MesClient.MachinePhasePlacement.ResumeSetupAsync(placement.Id, SelectedOperatorId, ct),
                MachinePhasePlacementStatus.WorkPaused => await MesClient.MachinePhasePlacement.ResumeWorkAsync(placement.Id, SelectedOperatorId, ct),
                _ => null,
            };

            if (result is null)
            {
                ErrorService.AddError(UiResources.Action_ErrorOpenSessionFailed);
                return;
            }

            await LoadContextAsync();
        });
    }

    private async Task ClosePlacementAsync(MachinePhasePlacementDto placement)
    {
        await RunAsync(async ct =>
        {
            var result = await MesClient.MachinePhasePlacement.CloseAsync(placement.Id, ct);
            if (result is null)
            {
                ErrorService.AddError(UiResources.Action_ErrorOpenSessionFailed);
                return;
            }

            await LoadContextAsync();
        });
    }

    private Task ShowShiftScreenAsync()
    {
        ViewModel.Screen = ActionScreen.CheckIn;
        ViewModel.Notify();
        return Task.CompletedTask;
    }

    private async Task OnShiftChangedAsync()
    {
        ViewModel.Screen = ActionScreen.Main;
        ViewModel.Notify();
        await LoadContextAsync();
    }

    private async Task ShowDeclareAsync()
    {
        await RunAsync(async ct =>
        {
            if (ViewModel.OpenSession is null)
            {
                ErrorService.AddError(UiResources.Action_ErrorOpenSessionFailed);
                return;
            }

            ViewModel.ConfirmedQty = 0;
            ViewModel.ScrapQty = 0;
            ViewModel.Screen = ActionScreen.Declare;
            ViewModel.Notify();
            await Task.CompletedTask;
        });
    }

    private async Task ConfirmDeclareAsync()
    {
        if (ViewModel.ActivePhase is null || ViewModel.ConfirmedQty <= 0) return;

        await RunAsync(async ct =>
        {
            await MesClient.ProductionDeclaration.CreateAsync(new ProductionDeclarationDto
            {
                ProductionOrderPhaseId = ViewModel.ActivePhase.Id,
                MachineId = Id,
                OperatorId = SelectedOperatorId ?? ViewModel.OpenSession?.OperatorId ?? 0,
                ConfirmedQuantity = ViewModel.ConfirmedQty,
                ScrapQuantity = ViewModel.ScrapQty,
                DeclarationDate = DateTimeOffset.UtcNow,
            }, ct);

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
                if (!await EnsureOperatorSelectedAsync(ct, onlyPresent: true)) return;

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
            if (!await EnsureOperatorSelectedAsync(ct, onlyPresent: true)) return;

            var result = await MesClient.MachineState.CreateAsync(new MachineStateDto
            {
                MachineId = Id,
                Status = MachineStatus.Running,
                EventTime = DateTimeOffset.UtcNow,
                Source = "Terminal",
                OperatorId = SelectedOperatorId,
            }, ct);

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

        if (ResumeStartWorkAfterOperatorSelection)
        {
            ResumeStartWorkAfterOperatorSelection = false;
            await StartWorkAsync();
        }
    }

    private Task CloseOperatorDialog()
    {
        IsOperatorDialogOpen = false;
        ResumeStartWorkAfterOperatorSelection = false;
        return Task.CompletedTask;
    }

    private async Task OnStopReasonSelectedAsync(int stopReasonId)
    {
        IsStopReasonDialogOpen = false;

        await RunAsync(async ct =>
        {
            if (!await EnsureOperatorSelectedAsync(ct, onlyPresent: true)) return;

            var now = DateTimeOffset.UtcNow;
            var stateResult = await MesClient.MachineState.CreateAsync(new MachineStateDto
            {
                MachineId = Id,
                Status = MachineStatus.Stopped,
                EventTime = now,
                Source = "Terminal",
                OperatorId = SelectedOperatorId,
            }, ct);

            if (stateResult.Success)
            {
                ViewModel.CurrentState = stateResult.Data;
            }

            await MesClient.MachineStop.CreateAsync(new MachineStopDto
            {
                MachineId = Id,
                ProductionOrderPhaseId = ViewModel.ActivePhase?.Id,
                MachineStopReasonId = stopReasonId,
                StartDate = now,
                EndDate = null,
            }, ct);

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

        if (ViewModel.PendingSessionType.HasValue)
        {
            var type = ViewModel.PendingSessionType.Value;
            await StartSessionOfType(type);
            return;
        }

        await RunAsync(async ct =>
        {
            var placement = ViewModel.OpenPlacements.FirstOrDefault(x => x.ProductionOrderPhaseId == phase.Id);
            if (placement is not null)
            {
                await SelectPlacementAsync(placement, ct);
            }
        });

        ViewModel.Screen = ActionScreen.Main;
        ViewModel.Notify();
    }

    private void GoBack()
    {
        if (ViewModel.Screen == ActionScreen.PhasePicker)
            ViewModel.ActivePhase = null;

        ViewModel.Screen = ActionScreen.Main;
        ViewModel.Notify();
    }

    private void NavigateHome() => Navigation.NavigateTo("/");
}
