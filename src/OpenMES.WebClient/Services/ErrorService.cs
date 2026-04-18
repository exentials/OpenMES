using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace OpenMES.WebClient.Services;

/// <summary>
/// Default implementation of IErrorService.
/// Manages error state with auto-dismiss capability using a background timer.
/// Thread-safe for concurrent calls from different components.
/// 
/// WEB-002: Phase 1 - Error Infrastructure
/// </summary>
public class ErrorService : IErrorService
{
    private string? _currentErrorMessage;
    private ErrorSeverity _currentSeverity = ErrorSeverity.Error;
    private DateTime? _errorTimestamp;
    private CancellationTokenSource? _autoDismissCts;
    private readonly object _lockObject = new();

    public string? CurrentErrorMessage
    {
        get => _currentErrorMessage;
        private set
        {
            if (_currentErrorMessage != value)
            {
                _currentErrorMessage = value;
                OnPropertyChanged();
            }
        }
    }

    public ErrorSeverity CurrentSeverity
    {
        get => _currentSeverity;
        private set
        {
            if (_currentSeverity != value)
            {
                _currentSeverity = value;
                OnPropertyChanged();
            }
        }
    }

    public DateTime? ErrorTimestamp
    {
        get => _errorTimestamp;
        private set
        {
            if (_errorTimestamp != value)
            {
                _errorTimestamp = value;
                OnPropertyChanged();
            }
        }
    }

    public bool HasError => !string.IsNullOrEmpty(CurrentErrorMessage);

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Add an error message with optional auto-dismiss.
    /// If an error is already displayed, it will be replaced.
    /// Thread-safe.
    /// </summary>
    public void AddError(string message, ErrorSeverity severity = ErrorSeverity.Error, int autoDismissMs = 0)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        lock (_lockObject)
        {
            // Cancel any pending auto-dismiss
            CancelAutoDismiss();

            CurrentErrorMessage = message;
            CurrentSeverity = severity;
            ErrorTimestamp = DateTime.UtcNow;

            // Schedule auto-dismiss if requested
            if (autoDismissMs > 0)
            {
                _autoDismissCts = new CancellationTokenSource();
                _ = AutoDismissAsync(autoDismissMs, _autoDismissCts.Token);
            }
        }
    }

    /// <summary>
    /// Clear the current error message and cancel any pending auto-dismiss.
    /// Thread-safe.
    /// </summary>
    public void ClearError()
    {
        lock (_lockObject)
        {
            CancelAutoDismiss();
            CurrentErrorMessage = null;
            CurrentSeverity = ErrorSeverity.Error;
            ErrorTimestamp = null;
        }
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void CancelAutoDismiss()
    {
        if (_autoDismissCts != null)
        {
            _autoDismissCts.Cancel();
            _autoDismissCts.Dispose();
            _autoDismissCts = null;
        }
    }

    private async Task AutoDismissAsync(int delayMs, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(delayMs, cancellationToken);
            ClearError();
        }
        catch (OperationCanceledException)
        {
            // Expected when ClearError is called manually
        }
    }
}
