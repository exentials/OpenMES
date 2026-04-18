using System.ComponentModel;
using OpenMES.WebClient.Models;

namespace OpenMES.WebClient.Services;

/// <summary>
/// Service for managing operator terminal sessions with inactivity timeout.
/// Handles session lifecycle: start, activity refresh, timeout detection, and logout.
/// 
/// WEB-003: Phase 1 - Session Management
/// </summary>
public interface ISessionService : INotifyPropertyChanged
{
    /// <summary>
    /// Current terminal identity (operator info and auth token).
    /// </summary>
    TerminalIdentity CurrentIdentity { get; }

    /// <summary>
    /// Whether a session is currently active (authenticated and not expired).
    /// </summary>
    bool IsSessionActive { get; }

    /// <summary>
    /// Minutes remaining until inactivity timeout (30 min total).
    /// Returns 0 if session expired.
    /// </summary>
    int MinutesUntilTimeout { get; }

    /// <summary>
    /// Whether timeout warning is displayed (5 minutes remaining).
    /// </summary>
    bool ShowTimeoutWarning { get; }

    /// <summary>
    /// Start a new session for the given operator.
    /// Initializes session timing and activity tracking.
    /// </summary>
    /// <param name="operatorName">Name of the logged-in operator</param>
    /// <param name="authToken">JWT authentication token</param>
    Task StartSessionAsync(string operatorName, string authToken);

    /// <summary>
    /// Restore an existing session from stored identity data.
    /// Used when the page reloads but the user is still authenticated.
    /// </summary>
    /// <param name="operatorName">Name of the logged-in operator</param>
    /// <param name="authToken">JWT authentication token</param>
    Task RestoreSessionAsync(string operatorName, string authToken);

    /// <summary>
    /// Refresh last activity time (called on user interaction).
    /// Resets the inactivity timer.
    /// </summary>
    void RefreshActivity();

    /// <summary>
    /// Get current session status.
    /// </summary>
    SessionStatus GetSessionStatus();

    /// <summary>
    /// End the current session (logout).
    /// Clears identity and resets timing.
    /// </summary>
    Task EndSessionAsync();

    /// <summary>
    /// Event raised when session timeout is imminent (5 min remaining).
    /// </summary>
    event EventHandler<SessionTimeoutWarningEventArgs>? TimeoutWarningRaised;

    /// <summary>
    /// Event raised when session has expired due to inactivity.
    /// </summary>
    event EventHandler? SessionExpired;
}

/// <summary>
/// Current session state information.
/// </summary>
public class SessionStatus
{
    public bool IsActive { get; set; }
    public bool IsExpired { get; set; }
    public bool ShowWarning { get; set; }
    public int MinutesRemaining { get; set; }
    public DateTime? SessionStartTime { get; set; }
    public DateTime? LastActivityTime { get; set; }
}

/// <summary>
/// Event args for timeout warning.
/// </summary>
public class SessionTimeoutWarningEventArgs : EventArgs
{
    public int MinutesRemaining { get; set; }
    public DateTime TimeoutTime { get; set; }
}
