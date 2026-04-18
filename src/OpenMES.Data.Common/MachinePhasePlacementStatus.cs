using System.Text.Json.Serialization;

namespace OpenMES.Data.Common;

/// <summary>
/// Represents the lifecycle state of a production phase placed on a machine.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<MachinePhasePlacementStatus>))]
public enum MachinePhasePlacementStatus : byte
{
    /// <summary>Phase is placed on the machine and waiting for setup/work start.</summary>
    Placed = 0,

    /// <summary>Setup activity is currently in progress.</summary>
    InSetup = 1,

    /// <summary>Setup activity is paused/suspended.</summary>
    SetupPaused = 2,

    /// <summary>Work activity is currently in progress.</summary>
    InWork = 3,

    /// <summary>Work activity is paused/suspended.</summary>
    WorkPaused = 4,

    /// <summary>Phase placement has been closed/unplaced from the machine.</summary>
    Closed = 9,
}
