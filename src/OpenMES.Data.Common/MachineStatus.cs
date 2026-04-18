using System.Text.Json.Serialization;

namespace OpenMES.Data.Common;

/// <summary>
/// Defines various statuses that a machine or system can have, represented as byte values.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<MachineStatus>))]
public enum MachineStatus : byte
{
	/// <summary>
	/// Represents the idle state, typically indicating no activity or operation is occurring.
	/// </summary>
	Idle = 0,
	/// <summary>
	/// Represents the state of an operation or process that is currently running.
	/// </summary>
	Running = 1,
	/// <summary>
	/// Indicates that the operation or process is in a stopped state.
	/// </summary>
	Stopped = 2,
	/// <summary>
	/// Represents a fault state in the machine.
	/// </summary>
	Fault = 3,
	/// <summary>
	/// Represents the setup state in the application's workflow.
	/// </summary>
	Setup = 4,
	/// <summary>
	/// Represents a state where the entity is blocked and unable to proceed.
	/// </summary>
	Blocked = 5,
	/// <summary>
	/// Represents a state where resources are insufficient or unavailable, leading to a halt in operations.
	/// </summary>
	Starved = 6,
	/// <summary>
	/// Represents the maintenance mode status of the machine.
	/// </summary>
	Maintenance = 7,
	/// <summary>
	/// Specifies that the operation mode is manual, requiring explicit user intervention or input.
	/// </summary>
	Manual = 8,

}
