using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using OpenMES.Data.Common;
using OpenMES.Data.Contexts;
using OpenMES.Data.Entities;
using OpenMES.Data.Helpers;

namespace OpenMES.MigrationService;

internal class MesMigrationWorker(
	IServiceProvider serviceProvider,
	ILogger<MesMigrationWorker> logger,
	CompletionCoordinator coordinator) : BackgroundService
{
	public const string ActivitySourceName = "MES Migrations";
	private static readonly ActivitySource Activity = new(ActivitySourceName);
	private readonly ILogger<MesMigrationWorker> _logger = logger;
	private readonly CompletionCoordinator _coordinator = coordinator;

	protected override async Task ExecuteAsync(CancellationToken cancellationToken)
	{
		using var activity = Activity.StartActivity("Migrating database", ActivityKind.Client);
		try
		{
			using var scope = serviceProvider.CreateScope();
			var dbContext = scope.ServiceProvider.GetRequiredService<OpenMESDbContext>();

			await dbContext.Database.EnsureDeletedAsync(cancellationToken);
			//await dbContext.Database.EnsureCreatedAsync(cancellationToken);

			await MigrateDataAsync(dbContext, cancellationToken);

			//await MesDataSeeding.SeedDataAsync(dbContext, cancellationToken);
		}
		catch (OperationCanceledException ex)
		{
			activity?.AddException(ex);
			_logger.LogWarning("Migration operation was cancelled.");
		}
		catch (Exception ex)
		{
			activity?.AddException(ex);
			throw;
		}

		_coordinator.ReportCompleted();
	}

	private static async Task MigrateDataAsync(OpenMESDbContext dbContext, CancellationToken cancellationToken)
	{
		var strategy = dbContext.Database.CreateExecutionStrategy();
		await strategy.ExecuteAsync(async () =>
		{
			await dbContext.Database.MigrateAsync(cancellationToken);
		});
	}
}