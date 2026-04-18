using OpenMES.Data.Common;
using OpenMES.Data.Dtos;

namespace OpenMES.WebClient.ViewModels;

/// <summary>
/// ViewModel for the Action page — the main operator screen for a specific machine.
/// Holds the current machine state, open session, active phase and drives which
/// action buttons are shown.
/// </summary>
public class ActionViewModel : ViewModelBase
{
    // ── Loaded context ─────────────────────────────────────────────────────
    public MachineDto? Machine { get; set; }
    public MachineStateDto? CurrentState { get; set; }
    public WorkSessionDto? OpenSession { get; set; }
    public OperatorShiftDto? OperatorShift { get; set; }
    public ProductionOrderPhaseDto? ActivePhase { get; set; }
    public List<ProductionDeclarationDto> LastDeclarations { get; set; } = [];

    // ── UI state ───────────────────────────────────────────────────────────
    public ActionScreen Screen { get; set; } = ActionScreen.Main;

    /// <summary>
    /// Remembers the session type chosen in SessionTypePicker so that
    /// OnPhaseSelectedAsync can complete the open-session flow after the
    /// user picks a phase.
    /// </summary>
    public WorkSessionType? PendingSessionType { get; set; }

    // ── Declaration inputs ─────────────────────────────────────────────────
    public decimal ConfirmedQty { get; set; }
    public decimal ScrapQty { get; set; }

    // ── Derived helpers ────────────────────────────────────────────────────
    public MachineStatus Status => CurrentState?.Status ?? MachineStatus.Idle;

    public bool IsOperatorPresent =>
        OperatorShift?.EventType is OperatorEventType.CheckIn or OperatorEventType.BreakEnd;

    public bool IsOperatorOnBreak =>
        OperatorShift?.EventType == OperatorEventType.BreakStart;

    public bool HasOpenSession => OpenSession is not null;

    public bool CanDeclare =>
        HasOpenSession && OpenSession!.SessionType == WorkSessionType.Work
        && ActivePhase is not null;

    public void Notify() => NotifyStateChanged();
}

/// <summary>Tracks which sub-screen is active within the Action page.</summary>
public enum ActionScreen
{
    Main,           // Context buttons grid
    Declare,        // NumericPad for production declaration
    CheckIn,        // Operator check-in (badge/ID entry)
    StopReason,     // Machine stop reason selection
    SessionTypePicker,
    PhasePicker,    // Phase/bolla selection before opening a session
}
