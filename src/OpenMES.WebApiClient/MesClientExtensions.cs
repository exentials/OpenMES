using OpenMES.WebApiClient;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Microsoft.Extensions.DependencyInjection;
#pragma warning restore IDE0130 // Namespace does not match folder structure
public static class MesClientExtensions
{
	/// <summary>
	/// Adds and configures an <see cref="MesClient"/> instance to the specified <see cref="IServiceCollection"/>.
	/// </summary>
	/// <remarks>This method registers an <see cref="HttpClient"/> for the <see cref="MesClient"/> type, setting its
	/// base address to a URI constructed using the specified <paramref name="connectionName"/>. The base address must be a
	/// valid URI.</remarks>
	/// <param name="services">The <see cref="IServiceCollection"/> to which the <see cref="MesClient"/> will be added.</param>
	/// <param name="connectionName">The name of the connection used to configure the <see cref="MesClient"/>. This value is used to construct the base
	/// address for the HTTP client.</param>
	/// <returns>The updated <see cref="IServiceCollection"/> instance.</returns>
	public static IServiceCollection AddMesClient(this IServiceCollection services, string connectionName)
	{
		services.AddHttpClient(nameof(MesClient), configureClient =>
		{
			configureClient.BaseAddress = new Uri($"https+http://{connectionName}");
		});

		services.AddScoped<MesClient>(sp =>
		{
			var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
			return new MesClient(httpClientFactory.CreateClient(nameof(MesClient)));
		});

		return services;
	}
}
