using Tgs.Agent.Dividend.Domain.Models;

namespace Tgs.Agent.Dividend.Domain.Interfaces.Repositories;

public interface IContractRepository
{
    IAsyncEnumerable<AgentDividendConditionModel> GetAgentDividendConditionsAsync(
        Guid agentId,
        CancellationToken cancellationToken);

    Task CreateAgentContractSnapshotAsync(Guid id, string data, CancellationToken cancellationToken);
}