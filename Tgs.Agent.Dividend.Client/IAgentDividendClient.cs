using Tgs.Agent.Dividend.Client.Models;

namespace Tgs.Agent.Dividend.Client;

public interface IAgentDividendClient
{
    IAsyncEnumerable<AgentDividendRecordModel?> GetAgentDividendRecordsAsync(GetAgentDividendRecordsRequest request,
        CancellationToken cancellationToken);

    IAsyncEnumerable<AgentDividendModel?> GetPreCalculateAgentDividend(Guid brandId,
        CancellationToken cancellationToken);

    Task<AgentDividendBeforehandResponse> GetAgentDividendBeforehandAsync(Guid brandId,
        Guid agentId,
        CancellationToken cancellationToken);

    Task GenerateAgentDividend(CancellationToken cancellationToken);

    Task GenerateAgentDividend(Guid brandId, CancellationToken cancellationToken);

    Task SendAgentDividendsAsync(Guid[] agentDividendRecordIds,
        string editorUsername,
        string? remark,
        CancellationToken cancellationToken);
}