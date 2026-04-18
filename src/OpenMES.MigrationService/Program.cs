using Microsoft.EntityFrameworkCore;
using OpenMES.Data.Contexts;
using OpenMES.MigrationService;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddHostedService<IdentityMigrationWorker>();
builder.Services.AddHostedService<MesMigrationWorker>();
builder.Services.AddSingleton<CompletionCoordinator>(sp =>
{
	var lifetime = sp.GetRequiredService<IHostApplicationLifetime>();
	return new CompletionCoordinator(2, lifetime);
});


builder.Services.AddOpenTelemetry()
	.WithTracing(tracing => tracing.AddSource(IdentityMigrationWorker.ActivitySourceName))
	.WithTracing(tracing => tracing.AddSource(MesMigrationWorker.ActivitySourceName));

var dbProvider = builder.Configuration.GetValue("DbProvider", "Pgsql");

if (dbProvider == "Pgsql")
{
	//builder.Services.AddNpgsql<OpenMESDbContext>(builder.Configuration.GetConnectionString("openmes-db"));
	builder.Services.AddDbContext<OpenMESIdentityDbContext>(options =>
	{
		options.UseNpgsql(
			builder.Configuration.GetConnectionString("openmes-identity-db"),
			sqlOptions => sqlOptions.MigrationsAssembly(typeof(OpenMES.Data.Pgsql.IMarker).Assembly.FullName)
		);
	});

	builder.Services.AddDbContext<OpenMESDbContext>(options =>
	{
		options
			.UseNpgsql(
				builder.Configuration.GetConnectionString("openmes-db"),
				sqlOptions => sqlOptions.MigrationsAssembly(typeof(OpenMES.Data.Pgsql.IMarker).Assembly.FullName)
			)
			.UseAsyncSeeding(async (context, _, cancellationToken) => await MesDataSeeding.SeedDataAsync(context, cancellationToken));
	});
}
else if (dbProvider == "SqlServer")
{
	var cs = builder.Configuration.GetConnectionString("openmes-db");

	builder.Services.AddDbContext<OpenMESIdentityDbContext>(options =>
	{
		options.UseSqlServer(
			builder.Configuration.GetConnectionString("openmes-identity-db"),
			sqlOptions => sqlOptions.MigrationsAssembly(typeof(OpenMES.Data.SqlServer.IMarker).Assembly.FullName)
		);
	});
	builder.Services.AddDbContext<OpenMESDbContext>(options =>
	{
		options.UseSqlServer(
			builder.Configuration.GetConnectionString("openmes-db"),
			sqlOptions => sqlOptions.MigrationsAssembly(typeof(OpenMES.Data.SqlServer.IMarker).Assembly.FullName)
		)
		.UseAsyncSeeding(async (context, _, cancellationToken) => await MesDataSeeding.SeedDataAsync(context, cancellationToken));		
	});
}
else
{
	throw new InvalidOperationException($"Unsupported database provider: {dbProvider}");
}


var host = builder.Build();
host.Run();
