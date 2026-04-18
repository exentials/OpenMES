using System.Security.Cryptography;

namespace OpenMES.Data.Helpers;
public static class Helpers
{
	/// <summary>
	/// Computes the MD5 hash of the specified input string and returns it as a hexadecimal string.
	/// </summary>
	/// <remarks>This method uses the MD5 cryptographic hash function to compute the hash of the input string.  The
	/// resulting hash is returned as an uppercase hexadecimal string.</remarks>
	/// <param name="input">The input string to compute the MD5 hash for. Cannot be <see langword="null"/>.</param>
	/// <returns>A hexadecimal string representation of the MD5 hash of the input string.</returns>
	public static string CalculateMD5(string input)
	{
		byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
		byte[] hashBytes = MD5.HashData(inputBytes);
		return Convert.ToHexString(hashBytes);
	}
}
