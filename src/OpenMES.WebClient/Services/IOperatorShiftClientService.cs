using OpenMES.Data.Common;
using OpenMES.Data.Dtos;
using OpenMES.WebApiClient.Interfaces;

namespace OpenMES.WebClient.Services;

public interface IOperatorShiftClientService
{
	Task EnsureAuthenticatedAsync(CancellationToken ct = default);
	Task<OperatorShiftSnapshot> LoadSnapshotAsync(CancellationToken ct = default);
	Task<IApiResult<OperatorShiftDto>> CreateShiftEventAsync(int operatorId, OperatorEventType eventType, CancellationToken ct = default);
	Task<bool> HasOpenWorkSessionAsync(int operatorId, CancellationToken ct = default);
	Task CloseOpenSessionsAsync(int operatorId, CancellationToken ct = default);
	bool CanApplyEvent(OperatorEventType? latestEventType, OperatorEventType targetEventType);
}

public sealed class OperatorShiftSnapshot
{
	public List<OperatorDto> Operators { get; init; } = [];
	public List<OperatorShiftDto> LatestShifts { get; init; } = [];
}
