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
            .AddKeyedTransient<IDividendCalculator, OneLayerDividendCalculator>(
                AgentDividendType.OneLayer.ToString())
            .AddKeyedTransient<IDividendCalculator, OneLayerDividendCalculator>(
                AgentDividendType.OneLayerV2.ToString())
            .AddKeyedTransient<IDividendCalculator, MultipleLayerDividendCalculator>(
                AgentDividendType.MultipleLayer.ToString());
            //.AddKeyedTransient<IDividendCalculator, OnePercentageDividendCalculator>(
            //    ContractDividendType.OnePercentage.ToString());

        return services;
    }
}