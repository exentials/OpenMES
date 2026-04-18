
using Scalar.Aspire;

string DB_PROVIDER = "Pgsql";   // This is just a placeholder, you can set it to "Pgsql" or "SqlServer" based on your requirements.

string DB_PROVIDER_ENV = "DbProvider";
string DB_DATA_VOLUME = "openmes-data";

var builder = DistributedApplication.CreateBuilder(args);

var compose = builder.AddDockerComposeEnvironment("env");

// Parametro JWT secret — il nome Aspire accetta solo lettere, cifre e trattini.
// In sviluppo viene letto da appsettings.Development.json sezione "Parameters:jwt-secret-key".
// In produzione viene iniettato come variabile d'ambiente jwt-secret-key (o tramite .env).
var jwtSecretKey = builder.AddParameter("jwt-secret-key", secret: true);

IResourceBuilder<IResourceWithConnectionString>? openmesIdentityDb = null;
IResourceBuilder<IResourceWithConnectionString>? openmesDb = null;

if (DB_PROVIDER == "Pgsql")
{
	var openmesDbPgsql = builder.AddPostgres(DB_PROVIDER)
		.WithLifetime(ContainerLifetime.Persistent)
		.WithDataVolume(DB_DATA_VOLUME)
		.WithEnvironment(DB_PROVIDER_ENV, DB_PROVIDER)
		.WithPgAdmin();

	openmesIdentityDb = openmesDbPgsql.AddDatabase("openmes-identity-db", databaseName: "openmes-identity");
	openmesDb = openmesDbPgsql.AddDatabase("openmes-db", databaseName: "openmes");

}
else if (DB_PROVIDER == "SqlServer")
{
	var openmesDbSqlServer = builder.AddSqlServer(DB_PROVIDER).WithHostPort(port: 14333)
		.WithLifetime(ContainerLifetime.Persistent)
		.WithDataVolume(DB_DATA_VOLUME)
		.WithEnvironment(DB_PROVIDER_ENV, DB_PROVIDER);

	openmesIdentityDb = openmesDbSqlServer.AddDatabase("openmes-identity-db", databaseName: "openmes-identity");
	openmesDb = openmesDbSqlServer.AddDatabase("openmes-db", databaseName: "openmes");
}
else
{
	throw new InvalidOperationException($"Unsupported database provider: {DB_PROVIDER}");
}

var registry = "exentials";

var migrations = builder.AddProject<Projects.OpenMES_MigrationService>("openmes-migrationservice")
		.WithReference(openmesIdentityDb)
		.WithReference(openmesDb)
		.WithEnvironment(DB_PROVIDER_ENV, DB_PROVIDER)
		.WaitFor(openmesIdentityDb)
		.WaitFor(openmesDb)
		.PublishAsDockerFile(configure =>
		{
			configure
				.WithDockerfile("..", "./OpenMES.MigrationService/Dockerfile")
				.WithImage($"{registry}/{configure.Resource.Name}");
		});

var webapi = builder.AddProject<Projects.OpenMES_WebApi>("openmes-webapi")
	.WithReference(openmesIdentityDb)
	.WithReference(openmesDb)
	.WaitFor(openmesIdentityDb)
	.WaitFor(openmesDb)
	.WaitForCompletion(migrations)
	.WithEnvironment(DB_PROVIDER_ENV, DB_PROVIDER)
	.WithEnvironment("Jwt__SecretKey", jwtSecretKey)
	.WithEnvironment("Jwt__Issuer", "openmes-webapi")
	.WithEnvironment("Jwt__Audience", "openmes-webadmin")
	.WithEnvironment("Jwt__ExpirationMinutes", "480")
	.PublishAsDockerFile(configure =>
	 {
		 configure
			.WithDockerfile("..", "./OpenMES.WebApi/Dockerfile")
			.WithImage($"{registry}/{configure.Resource.Name}");
	 });


builder.AddProject<Projects.OpenMES_WebAdmin>("openmes-webadmin")
	.WithExternalHttpEndpoints()
	.WithReference(webapi)
	.WaitFor(webapi)
	.WithEnvironment("Jwt__SecretKey", jwtSecretKey)
	.WithEnvironment("Jwt__Issuer", "openmes-webapi")
	.WithEnvironment("Jwt__Audience", "openmes-webadmin")
	.PublishAsDockerFile(configure =>
	{
		configure
			.WithDockerfile("..", "./OpenMes.WebAdmin/Dockerfile")
			.WithImage($"{registry}/{configure.Resource.Name}")
			.WithImageTag("latest");
	});

builder.AddProject<Projects.OpenMES_WebClient>("openmes-webclient")
	.WithReference(webapi)
	.WaitFor(webapi)
	.PublishAsDockerFile(configure =>
	{
		configure
			.WithDockerfile("..", "./OpenMes.WebClient/Dockerfile")
			.WithImage($"{registry}/{configure.Resource.Name}");
	});

var scalar = builder.AddScalarApiReference(options =>
	{
		options.Theme = ScalarTheme.Mars;
		options.DarkMode = true;
	});

scalar.WithApiReference(webapi).WaitFor(webapi);


builder.Build().Run();
