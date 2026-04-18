using System.Globalization;

namespace OpenMES.WebApi
{
	[Serializable]
	public class ProblemException(string error, string message) : Exception(message)
	{
		public string Error { get; } = error ?? throw new ArgumentNullException(nameof(error));
	}
}
