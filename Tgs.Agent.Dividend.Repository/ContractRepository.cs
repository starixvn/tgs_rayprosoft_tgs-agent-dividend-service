using Tgs.Agent.Dividend.Domain.Interfaces.Repositories;
using Tgs.Agent.Dividend.Domain.Models;
using Tgs.Agent.Dividend.Repository.Entities;

namespace Tgs.Agent.Dividend.Repository;

internal class ContractRepository(tgsContext context) : IContractRepository
{
    public IAsyncEnumerable<AgentDividendConditionModel> GetAgentDividendConditionsAsync(
        Guid agentId,
        CancellationToken cancellationToken)
    {
        return context.AgentDividendConditions
            .Where(x => x.AgentId == agentId)
            .Select(x => new AgentDividendConditionModel
            {
                Rank = x.Rank,
                ValidNumber = x.ActiveNumber,
                WinLossAmount = x.WinLossAmount,
                NetProfit = x.NetProfit,
                Percentage = x.DividendPercentage
            })
            .ToAsyncEnumerable();
    }

    public async Task CreateAgentContractSnapshotAsync(Guid id, string data, CancellationToken cancellationToken)
    {
        await context.AgentDividendSnapshots.AddAsync(new AgentDividendSnapshot
                {
                    Id = id,
                    Data = data,
                    CreatedOn = DateTime.UtcNow
                },
                cancellationToken)
            .ConfigureAwait(false);

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}