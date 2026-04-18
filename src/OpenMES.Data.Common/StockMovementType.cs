using System.Text.Json.Serialization;

namespace OpenMES.Data.Common;

[JsonConverter(typeof(JsonStringEnumConverter<StockMovementType>))]
public enum StockMovementType : byte
{
	/// <summary>Incoming stock (purchase, production output, return).</summary>
	GoodsReceipt = 0,
	/// <summary>Outgoing stock (sales, scrapping, consumption).</summary>
	GoodsIssue = 1,
	/// <summary>Manual quantity correction to align physical and system stock.</summary>
	Adjustment = 2,
	/// <summary>Stock transferred between two storage locations.</summary>
	Transfer = 3,
	/// <summary>Material issued to a production order phase.</summary>
	ProductionIssue = 4,
}
