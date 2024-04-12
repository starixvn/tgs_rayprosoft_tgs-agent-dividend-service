using Tgs.Agent.Dividend.Domain.Models;

namespace Tgs.Agent.Dividend.Domain.Interfaces.Repositories;

public interface IContractRepository
{
    IAsyncEnumerable<AgentDividendContractConditionModel> GetAgentDividendContractConditionsAsync(
        Guid agentDividendContractId,
        CancellationToken cancellationToken);

    Task CreateAgentContractSnapshotAsync(Guid id, string data, CancellationToken cancellationToken);
}