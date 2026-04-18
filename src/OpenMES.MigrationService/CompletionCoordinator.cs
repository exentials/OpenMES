using Microsoft.Extensions.Hosting;

namespace OpenMES.MigrationService;
internal class CompletionCoordinator(int totalServices, IHostApplicationLifetime hostApplicationLifetime)
{
	private int _completed = 0;
	private readonly int _totalServices = totalServices;
	private readonly IHostApplicationLifetime _hostApplicationLifetime = hostApplicationLifetime;

	public void ReportCompleted()
	{
		if (Interlocked.Increment(ref _completed) == _totalServices)
		{
			_hostApplicationLifetime.StopApplication();
		}
	}
}
