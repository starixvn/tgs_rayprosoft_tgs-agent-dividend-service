using Tgs.Agent.Dividend.Client;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IHttpClientBuilder AddAgentDividendClient(this IServiceCollection services)
        => services.AddHttpClient<IAgentDividendClient, AgentDividendClient>();
}