using System.Text.Json.Serialization;

namespace OpenMES.Data.Common;

[JsonConverter(typeof(JsonStringEnumConverter<OrderStatus>))]
public enum OrderStatus: byte
{
	/// <summary>The order has been created but not yet released for production. It may still be modified or deleted.</summary>
	Created = 0,
	/// <summary>
	/// The order has been released for production. It is now considered active and can be processed, but work may not have started yet. The order can still be modified or deleted at this stage.
	/// </summary>
	Released = 1,
	/// <summary>The order is in the setup phase, where necessary preparations are being made before production can begin.</summary>
	Setup = 2,
	/// <summary>The order is currently being processed in production.</summary>
	InProcess = 3,
	/// <summary>The order has been completed and all production activities are finished.</summary>
	Completed = 4,
	/// <summary>The order has been closed and is no longer active.</summary>
	Closed = 9,
	/// <summary>The order has been canceled and will not be processed.</summary>
	Canceled = 10
}
