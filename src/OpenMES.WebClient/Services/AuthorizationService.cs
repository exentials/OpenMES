using OpenMES.WebClient.Models;

namespace OpenMES.WebClient.Services;

/// <summary>
/// Service for checking authentication and authorization for route navigation.
/// Provides methods to verify user session status before allowing access to protected routes.
/// 
/// WEB-004: Phase 1 - Auth Guards
/// </summary>
public interface IAuthorizationService
{
    /// <summary>
    /// Check if user can navigate to the given route.
    /// Returns false if session is not active or expired.
    /// </summary>
    bool CanNavigate(string path);

    /// <summary>
    /// Check if user is currently authenticated and session is active.
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// Get current operator identity (or Anonymous if not authenticated).
    /// </summary>
    TerminalIdentity CurrentIdentity { get; }
}

/// <summary>
/// Default implementation of IAuthorizationService.
/// Uses ISessionService to verify session status.
/// 
/// WEB-004: Phase 1 - Auth Guards
/// </summary>
public class AuthorizationService : IAuthorizationService
{
    private readonly ISessionService _sessionService;

    /// <summary>
    /// Routes that don't require authentication.
    /// </summary>
    private static readonly string[] PublicRoutes = new[]
    {
        "/login",
        "/error"
    };

    public bool IsAuthenticated => _sessionService.IsSessionActive;
    public TerminalIdentity CurrentIdentity => _sessionService.CurrentIdentity;

    public AuthorizationService(ISessionService sessionService)
    {
        _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
    }

    /// <summary>
    /// Check if navigation to path is allowed.
    /// Public routes are always allowed.
    /// Protected routes require active session.
    /// </summary>
    public bool CanNavigate(string path)
    {
        // Normalize path
        var normalizedPath = path.StartsWith("/") ? path : "/" + path;

        // Check if route is public
        if (IsPublicRoute(normalizedPath))
            return true;

        // Protected route - require active session
        return IsAuthenticated;
    }

    private static bool IsPublicRoute(string path)
    {
        return PublicRoutes.Any(route => 
            path.Equals(route, StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith(route + "/", StringComparison.OrdinalIgnoreCase)
        );
    }
}
