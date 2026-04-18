using System.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenMES.Data.Contexts;

namespace OpenMES.MigrationService;

internal class IdentityMigrationWorker(
	IServiceProvider serviceProvider,
	ILogger<IdentityMigrationWorker> logger,
	CompletionCoordinator coordinator) : BackgroundService
{
	public const string ActivitySourceName = "Identity Migrations";
	private static readonly ActivitySource Activity = new(ActivitySourceName);
	private readonly ILogger<IdentityMigrationWorker> _logger = logger;	
    private readonly CompletionCoordinator _coordinator = coordinator;


	protected override async Task ExecuteAsync(CancellationToken cancellationToken)
	{
		using var activity = Activity.StartActivity("Migrating database", ActivityKind.Client);
		try
		{
			using var scope = serviceProvider.CreateScope();
			var dbContext = scope.ServiceProvider.GetRequiredService<OpenMESIdentityDbContext>();

			await dbContext.Database.EnsureDeletedAsync(cancellationToken);

			await MigrateIdentityAsync(dbContext, cancellationToken);
			await SeedIdentityAsync(dbContext, cancellationToken);
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

	private static async Task MigrateIdentityAsync(OpenMESIdentityDbContext dbContext, CancellationToken cancellationToken)
	{
		await dbContext.Database.EnsureDeletedAsync(cancellationToken);
		var strategy = dbContext.Database.CreateExecutionStrategy();
		await strategy.ExecuteAsync(async () =>
		{
			await dbContext.Database.MigrateAsync(cancellationToken);
		});
	}

	private static async Task SeedIdentityAsync(OpenMESIdentityDbContext dbContext, CancellationToken cancellationToken)
	{
		var strategy = dbContext.Database.CreateExecutionStrategy();
		await strategy.ExecuteAsync(async () =>
		{
			// Seed the database
			await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

			if (!dbContext.Users.Any())
			{
				var userId = Guid.CreateVersion7().ToString();
				var roleId = Guid.CreateVersion7().ToString();

				var hasher = new PasswordHasher<IdentityUser>();
				IdentityUser adminUser = new()
				{
					Id = userId,
					UserName = "admin@localhost.local",
					NormalizedUserName = "ADMIN@LOCALHOST.LOCAL",
					Email = "admin@localhost.local",
					NormalizedEmail = "ADMIN@LOCALHOST.LOCAL",
					SecurityStamp = Guid.CreateVersion7().ToString(),
				};
				adminUser.PasswordHash = hasher.HashPassword(adminUser, "Admin@123");
				IdentityRole adminRole = new()
				{
					Id = roleId,
					Name = "admin",
					NormalizedName = "ADMIN",
				};
				IdentityUserRole<string> adminUserRole = new()
				{
					RoleId = roleId,
					UserId = userId
				};
				dbContext.Users.Add(adminUser);
				dbContext.Roles.Add(adminRole);
				dbContext.UserRoles.Add(adminUserRole);
			}

			await dbContext.SaveChangesAsync(cancellationToken);
			await transaction.CommitAsync(cancellationToken);
		});
	}

}