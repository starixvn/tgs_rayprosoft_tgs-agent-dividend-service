using Tgs.Agent.Dividend.Domain.Models;

namespace Tgs.Agent.Dividend.Domain.Interfaces;

public interface IDividendCalculator
{
    IAsyncEnumerable<AgentPlayerAggregateModel>
        GetAgentPlayerAggregateAsync(int idNumber,
            BrandModel brand,
            AgentModel agent,
            CancellationToken cancellationToken);

    IAsyncEnumerable<AgentDividendMultipleLayerModel>
        GenerateMultipleLayerAsync(
            BrandModel brand,
            AgentModel agent,
            AgentPlayerAggregateModel agentPlayerAggregate,
            bool preCalculation,
            CancellationToken cancellationToken);

    decimal GetAgentDividendAmount(AgentPlayerAggregateModel agentPlayerAggregate,
        IEnumerable<AgentDividendMultipleLayerModel> multipleLayer,
        CancellationToken cancellation);

    IEnumerable<AgentDividendMultipleLayerModel> GetMultipleLayer(AgentPlayerAggregateModel agentPlayerAggregate,
        IEnumerable<AgentDividendMultipleLayerModel> multipleLayer,
        CancellationToken cancellation);
}