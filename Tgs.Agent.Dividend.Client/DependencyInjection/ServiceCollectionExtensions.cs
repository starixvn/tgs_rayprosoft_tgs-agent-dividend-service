using Microsoft.Extensions.Options;
using Tgs.Agent.Dividend.Client;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IHttpClientBuilder AddAgentDividendClient(this IServiceCollection services,
        Action<AgentDividnedOptions, IServiceProvider> configure)
    {
        return services.AddOptions<AgentDividnedOptions>()
            .Configure(configure)
            .Services.AddHttpClient<IAgentDividendClient, AgentDividendClient>((sp, c) =>
            {
                var options = sp.GetRequiredService<IOptions<AgentDividnedOptions>>().Value;
                c.BaseAddress = options.BaseUrl;
            });
    }
}