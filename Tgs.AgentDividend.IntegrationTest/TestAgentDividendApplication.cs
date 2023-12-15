using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTelemetry.Exporter;
using Tgs.Agent.Dividend.Repository.Entities;

namespace Tgs.Agent.Dividend.IntegrationTest;

extern alias AgentDividend;

internal class TestAgentDividendApplication : WebApplicationFactory<AgentDividend.Program>
{
	private readonly Action<IWebHostBuilder>? _webHostConfigure;

	public TestAgentDividendApplication(Action<IWebHostBuilder>? webHostConfigure = null)
	{
		_webHostConfigure = webHostConfigure;
	}

	protected override void ConfigureWebHost(IWebHostBuilder builder)
	{
		_ = builder
			.ConfigureAppConfiguration(builder => builder.Sources.Clear())
			.ConfigureLogging(logging => logging.ClearProviders().AddConsole());

		_ = builder.ConfigureServices(services => services
			.AddSingleton<IConfigureOptions<OtlpExporterOptions>>(
				sp => new ConfigureNamedOptions<OtlpExporterOptions>(
					Options.DefaultName,
					exporterOptions =>
					{
						exporterOptions.TimeoutMilliseconds = 0;
						exporterOptions.ExportProcessorType = OpenTelemetry.ExportProcessorType.Batch;
						exporterOptions.Endpoint = new Uri("http://otel.rpgp.cc:4317");
					}))
			.Replace(ServiceDescriptor.Singleton(sp =>
			{
				var dbContextOptionsBuilder = new DbContextOptionsBuilder<tgsContext>();

				return new tgsContext(dbContextOptionsBuilder.Options);
			})));

		_webHostConfigure?.Invoke(builder);
	}
}
