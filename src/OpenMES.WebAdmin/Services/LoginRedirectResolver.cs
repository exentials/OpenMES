namespace OpenMES.WebAdmin.Services;

/// <summary>
/// Resolves post-login navigation target while preventing open redirects.
/// </summary>
public static class LoginRedirectResolver
{
    public static string ResolveTarget(string? returnUrl, string baseUri)
    {
        if (string.IsNullOrEmpty(returnUrl))
        {
            return "/";
        }

        var decoded = Uri.UnescapeDataString(returnUrl);

        if (Uri.TryCreate(decoded, UriKind.Absolute, out var absoluteUri))
        {
            var baseHost = new Uri(baseUri).Host;
            if (!string.Equals(absoluteUri.Host, baseHost, StringComparison.OrdinalIgnoreCase))
            {
                return "/";
            }

            return absoluteUri.PathAndQuery + absoluteUri.Fragment;
        }

        if (Uri.TryCreate(decoded, UriKind.Relative, out _))
        {
            return decoded.StartsWith("/", StringComparison.Ordinal) ? decoded : "/" + decoded;
        }

        return "/";
    }
}
