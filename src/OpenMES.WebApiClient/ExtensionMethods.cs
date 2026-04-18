using System.Text.Json;

namespace OpenMES.WebApiClient;

public static class ExtensionMethods
{
	public static T DeepCopy<T>(this T self)
	{
		var serialized = JsonSerializer.Serialize(self);
		return JsonSerializer.Deserialize<T>(serialized) ?? default!;
	}
}
