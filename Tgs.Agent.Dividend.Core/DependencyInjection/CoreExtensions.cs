using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Tgs.Agent.Dividend.Core.Distributions;
using Tgs.Agent.Dividend.Domain.Enums;
using Tgs.Agent.Dividend.Domain.Interfaces;

namespace Tgs.Agent.Dividend.Core.DependencyInjection;

public static class CoreExtensions
{
    public static IServiceCollection AddCore(this IServiceCollection services)
    {
        _ = services
            .AddMediatR(r => r.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()))
            .AddKeyedTransient<IDistribution, OneLayerDistribution>(ContractDividendType.OneLayer.ToString())
            .AddKeyedTransient<IDistribution, MultipleLayerDistribution>(
                ContractDividendType.MultipleLayer.ToString())
            .AddKeyedTransient<IDistribution, OnePercentageDistribution>(
                ContractDividendType.OnePercentage.ToString());

        return services;
    }
}