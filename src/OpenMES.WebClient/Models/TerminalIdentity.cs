using System.ComponentModel;

namespace OpenMES.WebClient.Models;

/// <summary>
/// Represents the authenticated operator identity on the WebClient terminal.
/// Tracks authentication state and session timing for timeout management.
/// 
/// WEB-003: Session Management Phase 1
/// </summary>
public class TerminalIdentity : INotifyPropertyChanged
{
	private DateTime _sessionStartTime;
	private DateTime _lastActivityTime;

	public string Name { get; set; } = "Anonymous";
	public string AuthToken { get; set; } = string.Empty;
	public bool IsAuthenticated => !string.IsNullOrWhiteSpace(AuthToken);

	/// <summary>
	/// UTC time when the operator authenticated and session started.
	/// </summary>
	public DateTime SessionStartTime
	{
		get => _sessionStartTime;
		set
		{
			if (_sessionStartTime != value)
			{
				_sessionStartTime = value;
				OnPropertyChanged();
			}
		}
	}

	/// <summary>
	/// UTC time of last user activity (click, keypress, mousemove).
	/// Used to detect inactivity timeout.
	/// </summary>
	public DateTime LastActivityTime
	{
		get => _lastActivityTime;
		set
		{
			if (_lastActivityTime != value)
			{
				_lastActivityTime = value;
				OnPropertyChanged();
			}
		}
	}

	/// <summary>
	/// Check if session has expired based on inactivity timeout (30 minutes).
	/// </summary>
	public bool IsSessionExpired(int inactivityTimeoutMinutes = 30)
	{
		if (!IsAuthenticated)
			return true;

		var timeSinceActivity = DateTime.UtcNow - LastActivityTime;
		return timeSinceActivity.TotalMinutes >= inactivityTimeoutMinutes;
	}

	/// <summary>
	/// Get minutes remaining until timeout (30 min total).
	/// </summary>
	public int GetMinutesUntilTimeout(int inactivityTimeoutMinutes = 30)
	{
		if (!IsAuthenticated)
			return 0;

		var elapsed = DateTime.UtcNow - LastActivityTime;
		var remaining = inactivityTimeoutMinutes - (int)elapsed.TotalMinutes;
		return Math.Max(0, remaining);
	}

	public void SetIdentity(string name, string authToken)
	{
		if (!string.IsNullOrWhiteSpace(name))
		{
			Name = name;
		}
		if (!string.IsNullOrWhiteSpace(authToken))
		{
			AuthToken = authToken;
		}

		// WEB-003: Initialize session timing when authenticated
		var now = DateTime.UtcNow;
		SessionStartTime = now;
		LastActivityTime = now;
	}

	public event PropertyChangedEventHandler? PropertyChanged;

	protected void OnPropertyChanged(string? propertyName = null)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	public static TerminalIdentity Anonymous { get; } = new TerminalIdentity();
}
