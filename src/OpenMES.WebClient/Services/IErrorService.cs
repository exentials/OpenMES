using System.ComponentModel;

namespace OpenMES.WebClient.Services;

/// <summary>
/// Service for managing and displaying error messages in the WebClient application.
/// Provides centralized error handling with optional severity levels and auto-dismiss capabilities.
/// 
/// WEB-002: Phase 1 - Error Infrastructure
/// </summary>
public interface IErrorService : INotifyPropertyChanged
{
    /// <summary>
    /// Current error message to display, or null if no error.
    /// </summary>
    string? CurrentErrorMessage { get; }

    /// <summary>
    /// Severity level of the current error (Info, Warning, Error, Critical).
    /// </summary>
    ErrorSeverity CurrentSeverity { get; }

    /// <summary>
    /// Time when the current error was added (UTC).
    /// </summary>
    DateTime? ErrorTimestamp { get; }

    /// <summary>
    /// Add an error message with optional severity.
    /// Replaces any existing error message.
    /// </summary>
    /// <param name="message">The error message to display</param>
    /// <param name="severity">Severity level (default: Error)</param>
    /// <param name="autoDismissMs">Auto-dismiss after milliseconds (0 = manual dismiss only)</param>
    void AddError(string message, ErrorSeverity severity = ErrorSeverity.Error, int autoDismissMs = 0);

    /// <summary>
    /// Clear the current error message and notify subscribers.
    /// </summary>
    void ClearError();

    /// <summary>
    /// Check if an error is currently displayed.
    /// </summary>
    bool HasError { get; }
}

/// <summary>
/// Error severity levels for categorizing messages.
/// </summary>
public enum ErrorSeverity
{
    /// <summary>Informational message.</summary>
    Info = 0,

    /// <summary>Warning message.</summary>
    Warning = 1,

    /// <summary>Error message.</summary>
    Error = 2,

    /// <summary>Critical/fatal error requiring immediate attention.</summary>
    Critical = 3
}
