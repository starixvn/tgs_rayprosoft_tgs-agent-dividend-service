using Tgs.Agent.Dividend.Client;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IHttpClientBuilder AddAgentDividendClient(this IServiceCollection services)
    {
        return services.AddHttpClient<IAgentDividendClient, AgentDividendClient>();
    }

    public static IHttpClientBuilder AddBrandContractClient(this IServiceCollection services)
    {
        return services.AddHttpClient<IBrandContractClient, BrandContractClient>();
    }
}