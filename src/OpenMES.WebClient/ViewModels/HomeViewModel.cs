using OpenMES.Data.Common;
using OpenMES.Data.Dtos;

namespace OpenMES.WebClient.ViewModels;

public class HomeViewModel : ViewModelBase
{
	public IEnumerable<MachineDto> Machines { get; set; } = [];

	/// <summary>Current state per machine (machineId → dto). Loaded in parallel with Machines.</summary>
	public Dictionary<int, MachineStateDto> MachineStates { get; set; } = [];

	/// <summary>Open sessions per machine (machineId → first open session). Used to show active operator on card.</summary>
	public Dictionary<int, WorkSessionDto> OpenSessions { get; set; } = [];

	public MachineStatus GetStatus(int machineId)
		=> MachineStates.TryGetValue(machineId, out var s) ? s.Status : MachineStatus.Idle;

	public string? GetActiveOperator(int machineId)
		=> OpenSessions.TryGetValue(machineId, out var s) ? s.OperatorName : null;

	public WorkSessionType? GetSessionType(int machineId)
		=> OpenSessions.TryGetValue(machineId, out var s) ? s.SessionType : null;
}
