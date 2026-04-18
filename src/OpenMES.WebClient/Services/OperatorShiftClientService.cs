using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using OpenMES.Data.Common;
using OpenMES.Data.Dtos;
using OpenMES.Localization.Resources;
using OpenMES.WebApiClient;
using OpenMES.WebApiClient.Interfaces;

namespace OpenMES.WebClient.Services;

public class OperatorShiftClientService(
    MesClient mesClient,
    ProtectedLocalStorage localStore)
    : IOperatorShiftClientService
{
    public async Task EnsureAuthenticatedAsync(CancellationToken ct = default)
    {
        var authResult = await localStore.GetAsync<TerminalLoginResultDto>("auth");
        if (!authResult.Success || authResult.Value is null || string.IsNullOrWhiteSpace(authResult.Value.AuthToken))
        {
            throw new InvalidOperationException(UiResources.Error_HttpUnauthorized);
        }

        mesClient.SetAuthToken(authResult.Value.AuthToken);
    }

    public async Task<OperatorShiftSnapshot> LoadSnapshotAsync(CancellationToken ct = default)
    {
        await EnsureAuthenticatedAsync(ct);

        var operatorsTask = mesClient.Operator.ReadsAsync(0, 500, ct);
        var shiftsTask = mesClient.OperatorShift.ReadsAsync(0, 500, ct);

        await Task.WhenAll(operatorsTask, shiftsTask);

        var operators = operatorsTask.Result.Data?.Items?.ToList() ?? [];
        var latestShifts = shiftsTask.Result.Data?.Items?
            .GroupBy(x => x.OperatorId)
            .Select(g => g.OrderByDescending(s => s.EventTime).First())
            .ToList() ?? [];

        return new OperatorShiftSnapshot
        {
            Operators = operators,
            LatestShifts = latestShifts,
        };
    }

    public async Task<IApiResult<OperatorShiftDto>> CreateShiftEventAsync(
        int operatorId,
        OperatorEventType eventType,
        CancellationToken ct = default)
    {
        await EnsureAuthenticatedAsync(ct);

        return await mesClient.OperatorShift.CreateAsync(new OperatorShiftDto
        {
            OperatorId = operatorId,
            EventType = eventType,
            EventTime = DateTimeOffset.UtcNow,
            Source = "Terminal",
        }, ct);
    }

    public async Task<bool> HasOpenWorkSessionAsync(int operatorId, CancellationToken ct = default)
    {
        await EnsureAuthenticatedAsync(ct);

        var openSessions = await mesClient.WorkSession.GetOpenAsync(ct);
        return openSessions.Any(x => x.OperatorId == operatorId && x.SessionType == WorkSessionType.Work);
    }

    public async Task CloseOpenSessionsAsync(int operatorId, CancellationToken ct = default)
    {
        await EnsureAuthenticatedAsync(ct);

        var openSessions = await mesClient.WorkSession.GetOpenAsync(ct);
        var sessionsToClose = openSessions
            .Where(x => x.OperatorId == operatorId)
            .Select(x => x.Id)
            .ToList();

        foreach (var sessionId in sessionsToClose)
        {
            await mesClient.WorkSession.CloseSessionAsync(sessionId, ct);
        }
    }

    public bool CanApplyEvent(OperatorEventType? latestEventType, OperatorEventType targetEventType)
        => (targetEventType, latestEventType) switch
        {
            (OperatorEventType.CheckIn, null) => true,
            (OperatorEventType.CheckIn, OperatorEventType.CheckOut) => true,
            (OperatorEventType.BreakStart, OperatorEventType.CheckIn) => true,
            (OperatorEventType.BreakStart, OperatorEventType.BreakEnd) => true,
            (OperatorEventType.BreakEnd, OperatorEventType.BreakStart) => true,
            (OperatorEventType.CheckOut, OperatorEventType.CheckIn) => true,
            (OperatorEventType.CheckOut, OperatorEventType.BreakStart) => true,
            (OperatorEventType.CheckOut, OperatorEventType.BreakEnd) => true,
            _ => false,
        };
}
