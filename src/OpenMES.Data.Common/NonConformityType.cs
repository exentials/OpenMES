using System.Text.Json.Serialization;

namespace OpenMES.Data.Common;

[JsonConverter(typeof(JsonStringEnumConverter<NonConformityType>))]
public enum NonConformityType : byte
{
	/// <summary>Non-conformity detected internally during production.</summary>
	Internal = 0,
	/// <summary>Non-conformity reported by a customer after delivery.</summary>
	Customer = 1,
	/// <summary>Non-conformity originating from a supplier or incoming material.</summary>
	Supplier = 2,
}
