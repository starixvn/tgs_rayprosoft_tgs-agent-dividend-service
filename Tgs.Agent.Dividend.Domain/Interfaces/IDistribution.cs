using Tgs.Agent.Dividend.Domain.Models;

namespace Tgs.Agent.Dividend.Domain.Interfaces;

public interface IDistribution
{
    IAsyncEnumerable<(AgentPlayerAggregateModel agentPlayerAggregate, AgentDividendMultipleLayerModel[] multipleLayer)>
        AgentDividendGenerationAsync(int idNumber,
            BrandModel brand,
            AgentDividendContractModel contract,
            CancellationToken cancellationToken);

    decimal GetAgentDividendAmount(AgentPlayerAggregateModel agentPlayerAggregate,
        IEnumerable<AgentDividendMultipleLayerModel> multipleLayer,
        CancellationToken cancellation);

    IEnumerable<AgentDividendMultipleLayerModel> GetMultipleLayer(AgentPlayerAggregateModel agentPlayerAggregate,
        IEnumerable<AgentDividendMultipleLayerModel> multipleLayer,
        CancellationToken cancellation);

    //Task<CreateAgentDividendModel> GetDividendAsync(
    //    BrandModel brand, 
    //    AgentPlayerAggregateModel agentPlayerData,
    //    IAsyncEnumerable<AgentDividendMultipleLayerModel> agentDividendMultipleLayers,
    //    CancellationToken cancellationToken);

    //IAsyncEnumerable<CreateAgentDividendModel> GetNegativeDividendAsync(int idNumber, BrandModel brand, AgentDividendContractModel contract,
    //    IAsyncEnumerable<AgentPlayerAggregateModel> agentPlayerAggregates,
    //    CancellationToken cancellationToken);

    //IAsyncEnumerable<CreateAgentDividendModel> GetAllAgentDividend();
}