using MediatR;

namespace Tgs.Agent.Dividend.Domain.Models;

public record GetAgentDividendWithContractRequest(
    int Id,
    BrandModel Brand,
    AgentModel Agent,
    IAsyncEnumerable<AgentPlayerAggregateModel> Aggregate,
    AgentDividendContractModel Contract) : IStreamRequest<CreateAgentDividendModel>;