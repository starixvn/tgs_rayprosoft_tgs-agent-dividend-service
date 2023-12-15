using Microsoft.EntityFrameworkCore;
using Tgs.Agent.Dividend.Domain.Models;

namespace Tgs.Agent.Dividend.Domain.Interfaces.Repositories;

public interface IRepository
{
    IAsyncEnumerable<AgentModel> GetAgentsCacheAsyncEnumerable(Guid brandId, CancellationToken cancellationToken);

    IAsyncEnumerable<AgentModel> GetAgentsByPathCacheAsyncEnumerable(Guid brandId,
        LTree path,
        CancellationToken cancellationToken);

    IAsyncEnumerable<Guid> GetNonArchivedBrandIdsAsyncEnumerable(Guid? brandId);

    IAsyncEnumerable<AgentPlayerAggregateModel> GetAgentPlayerAggregatesCacheAsync(Guid brandId,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken);

    IAsyncEnumerable<AgentPlayerAggregateModel> GetAgentPlayerAggregatesForMultipleLayerCacheAsync(Guid brandId,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken);

    IAsyncEnumerable<AgentPlayerGameAggregateModel> GetAgentPlayerGameAggregatesCacheAsync(Guid brandId,
        DateTime from,
        DateTime to);

    IAsyncEnumerable<AgentPlayerDepositModel> GetAgentPlayerDepositsCacheAsync(Guid brandId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken);

    IAsyncEnumerable<AgentPlayerValidBetModel> GetAgentPlayerValidBetsCacheAsync(Guid brandId,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken);

    IAsyncEnumerable<AgentPlayerBetDaysModel> GetAgentPlayerBetCountsCacheAsync(Guid brandId,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken);

    IAsyncEnumerable<NegativeAgentDividendModel> GetNegativeAgentDividendsAsync(Guid agent);

    Task CreateAgentDividendsAsync(BrandModel brand,
        IEnumerable<CreateAgentDividendModel> agentDividends,
        IDictionary<int, int> sendNegativeDividendIds,
        CancellationToken cancellationToken);

    Task<BrandModel> GetBrandAsync(Guid brandId, CancellationToken cancellationToken);

    Task<int> GetMaxAgentDividendIdAsync(CancellationToken cancellationToken);

    Task UpdateSentOnAsync(int agentDividendId, CancellationToken cancellationToken);

    Task CreateAgentDividendRecordAsync(Guid brandId,
        Guid agentId,
        int agentDividendId,
        decimal agentDividendAmount,
        CancellationToken cancellationToken);

    void RemoveAllMemoryCacheAsync(CancellationToken cancellationToken);
}