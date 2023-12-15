// ReSharper disable CheckNamespace

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Tgs.Agent.Dividend.Domain.Interfaces;
using Tgs.Agent.Dividend.Domain.Interfaces.Repositories;
using Tgs.Agent.Dividend.Repository;
using Tgs.Agent.Dividend.Repository.Entities;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRepository(this IServiceCollection services,
        Action<IServiceProvider, DbContextOptionsBuilder> dbContextConfigure)
    {
        return services
            .AddDbContext<tgsContext>(dbContextConfigure)
            .AddTransient<IRepository, Repository>()
            .AddTransient<IContractRepository, ContractRepository>();
    }
}