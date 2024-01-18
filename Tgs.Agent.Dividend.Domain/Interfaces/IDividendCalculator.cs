using Tgs.Agent.Dividend.Domain.Models;

namespace Tgs.Agent.Dividend.Domain.Interfaces;

public interface IDividendCalculator
{
    IAsyncEnumerable<AgentPlayerAggregateModel>
        GetAgentPlayerAggregateAsync(int idNumber,
            BrandModel brand,
            AgentDividendContractModel contract,
            CancellationToken cancellationToken);

    IAsyncEnumerable<AgentDividendMultipleLayerModel>
        GenerateMultipleLayerAsync(
            BrandModel brand,
            AgentDividendContractModel contract,
            AgentPlayerAggregateModel agentPlayerAggregate,
            CancellationToken cancellationToken);

    //IEnumerable<AgentPlayerAggregateModel> GenerateAdditionalAgentPlayerAggregates(int idNumber,
    //    BrandModel brand,
    //    AgentDividendMultipleLayerModel childLayer,
    //    IEnumerable<AgentPlayerAggregateModel> allAgentPlayerAggregates,
    //    CancellationToken cancellationToken);

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