using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.JSInterop;
using OpenMES.Data.Dtos.Resources;
using OpenMES.Localization.Resources;
using OpenMES.WebClient.Models;

namespace OpenMES.WebClient.Services;

/// <summary>
/// Default implementation of ISessionService.
/// Manages operator session lifecycle with inactivity timeout (30 min) and warning (5 min).
/// Uses background timer to check for timeout every 10 seconds.
/// Thread-safe for concurrent calls.
/// 
/// WEB-003: Phase 1 - Session Management
/// </summary>
public class SessionService : ISessionService
{
    private readonly IErrorService _errorService;
    private TerminalIdentity _currentIdentity = TerminalIdentity.Anonymous;
    private bool _isSessionActive;
    private bool _showTimeoutWarning;
    private int _minutesUntilTimeout;
    
    private Timer? _inactivityCheckTimer;
    private bool _warningAlreadyShown;
    private readonly Lock _lockObject = new();

    // Session timeout configuration (in minutes)
    private const int INACTIVITY_TIMEOUT_MINUTES = 30;
    private const int WARNING_THRESHOLD_MINUTES = 5;
    private const int CHECK_INTERVAL_SECONDS = 10;

    public TerminalIdentity CurrentIdentity
    {
        get => _currentIdentity;
        private set
        {
            if (_currentIdentity != value)
            {
                _currentIdentity = value;
                OnPropertyChanged();
            }
        }
    }

    public bool IsSessionActive
    {
        get => _isSessionActive;
        private set
        {
            if (_isSessionActive != value)
            {
                _isSessionActive = value;
                OnPropertyChanged();
            }
        }
    }

    public bool ShowTimeoutWarning
    {
        get => _showTimeoutWarning;
        private set
        {
            if (_showTimeoutWarning != value)
            {
                _showTimeoutWarning = value;
                OnPropertyChanged();
            }
        }
    }

    public int MinutesUntilTimeout
    {
        get => _minutesUntilTimeout;
        private set
        {
            if (_minutesUntilTimeout != value)
            {
                _minutesUntilTimeout = value;
                OnPropertyChanged();
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler<SessionTimeoutWarningEventArgs>? TimeoutWarningRaised;
    public event EventHandler? SessionExpired;

    public SessionService(IErrorService errorService)
    {
        _errorService = errorService ?? throw new ArgumentNullException(nameof(errorService));
    }

    /// <summary>
    /// Start a new session for the operator.
    /// Initializes activity tracking and background timeout check.
    /// </summary>
    public async Task StartSessionAsync(string operatorName, string authToken)
    {
        lock (_lockObject)
        {
            CurrentIdentity = new TerminalIdentity();
            CurrentIdentity.SetIdentity(operatorName, authToken);
            IsSessionActive = true;
            ShowTimeoutWarning = false;
            _warningAlreadyShown = false;
            MinutesUntilTimeout = INACTIVITY_TIMEOUT_MINUTES;

            // Start background inactivity timer
            StartInactivityCheckTimer();
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Restore an existing session from stored identity data.
    /// Used when the page reloads but the user is still authenticated.
    /// </summary>
    public async Task RestoreSessionAsync(string operatorName, string authToken)
    {
        lock (_lockObject)
        {
            CurrentIdentity = new TerminalIdentity();
            CurrentIdentity.SetIdentity(operatorName, authToken);
            IsSessionActive = true;
            ShowTimeoutWarning = false;
            _warningAlreadyShown = false;
            MinutesUntilTimeout = INACTIVITY_TIMEOUT_MINUTES;

            // Start background inactivity timer
            StartInactivityCheckTimer();
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Refresh activity timestamp (call on user interaction).
    /// Resets the inactivity counter and hides warning if shown.
    /// </summary>
    [JSInvokable]
    public void RefreshActivity()
    {
        lock (_lockObject)
        {
            if (IsSessionActive && CurrentIdentity.IsAuthenticated)
            {
                CurrentIdentity.LastActivityTime = DateTime.UtcNow;
                ShowTimeoutWarning = false;
                _warningAlreadyShown = false;
                MinutesUntilTimeout = INACTIVITY_TIMEOUT_MINUTES;
            }
        }
    }

    /// <summary>
    /// Get current session status snapshot.
    /// </summary>
    public SessionStatus GetSessionStatus()
    {
        lock (_lockObject)
        {
            var isExpired = CurrentIdentity.IsSessionExpired(INACTIVITY_TIMEOUT_MINUTES);
            var minutesRemaining = CurrentIdentity.GetMinutesUntilTimeout(INACTIVITY_TIMEOUT_MINUTES);

            return new SessionStatus
            {
                IsActive = IsSessionActive,
                IsExpired = isExpired,
                ShowWarning = ShowTimeoutWarning,
                MinutesRemaining = minutesRemaining,
                SessionStartTime = CurrentIdentity.SessionStartTime,
                LastActivityTime = CurrentIdentity.LastActivityTime
            };
        }
    }

    /// <summary>
    /// End session and logout operator.
    /// </summary>
    public async Task EndSessionAsync()
    {
        lock (_lockObject)
        {
            StopInactivityCheckTimer();
            CurrentIdentity = TerminalIdentity.Anonymous;
            IsSessionActive = false;
            ShowTimeoutWarning = false;
            MinutesUntilTimeout = 0;
        }

        await Task.CompletedTask;
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void StartInactivityCheckTimer()
    {
        _inactivityCheckTimer = new Timer(
            callback: CheckInactivityAsync,
            state: null,
            dueTime: TimeSpan.FromSeconds(CHECK_INTERVAL_SECONDS),
            period: TimeSpan.FromSeconds(CHECK_INTERVAL_SECONDS)
        );
    }

    private void StopInactivityCheckTimer()
    {
        if (_inactivityCheckTimer != null)
        {
            _inactivityCheckTimer.Dispose();
            _inactivityCheckTimer = null;
        }
    }

    private void CheckInactivityAsync(object? state)
    {
        lock (_lockObject)
        {
            if (!IsSessionActive || !CurrentIdentity.IsAuthenticated)
                return;

            var minutesRemaining = CurrentIdentity.GetMinutesUntilTimeout(INACTIVITY_TIMEOUT_MINUTES);
            MinutesUntilTimeout = minutesRemaining;

            // Check if session expired
            if (CurrentIdentity.IsSessionExpired(INACTIVITY_TIMEOUT_MINUTES))
            {
                StopInactivityCheckTimer();
                IsSessionActive = false;
                ShowTimeoutWarning = false;
                
                _errorService.AddError(
                    UiResources.Session_ExpiredInactivity,
                    ErrorSeverity.Warning
                );

                SessionExpired?.Invoke(this, EventArgs.Empty);
            }
            // Check if warning should be shown (5 min remaining)
            else if (minutesRemaining <= WARNING_THRESHOLD_MINUTES && !_warningAlreadyShown)
            {
                ShowTimeoutWarning = true;
                _warningAlreadyShown = true;

                var timeoutTime = CurrentIdentity.LastActivityTime.AddMinutes(INACTIVITY_TIMEOUT_MINUTES);
                TimeoutWarningRaised?.Invoke(this, new SessionTimeoutWarningEventArgs
                {
                    MinutesRemaining = minutesRemaining,
                    TimeoutTime = timeoutTime
                });
            }
        }
    }
}
