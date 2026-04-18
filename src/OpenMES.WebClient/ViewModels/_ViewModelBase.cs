namespace OpenMES.WebClient.ViewModels;

public abstract class ViewModelBase
{
	public bool IsBusy { get; protected set; }
	public string? ErrorMessage { get; protected set; }
	public event Action? StateChanged;

	protected void NotifyStateChanged() => StateChanged?.Invoke();
}
