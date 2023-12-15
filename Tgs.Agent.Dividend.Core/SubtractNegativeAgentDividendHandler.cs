using MediatR;
using Tgs.Agent.Dividend.Core.Workers;
using Tgs.Agent.Dividend.Domain.Interfaces.Repositories;
using Tgs.Agent.Dividend.Domain.Models;

namespace Tgs.Agent.Dividend.Core;

internal class SubtractNegativeAgentDividendHandler(
    IAgentDividendTaskQueue queue,
    IMediator mediator,
    IRepository repository)
    : IRequestHandler<SubtractNegativeAgentDividendRequest, SubtractNegativeAgentDividendModel>
{
    public Task<SubtractNegativeAgentDividendModel> Handle(SubtractNegativeAgentDividendRequest request,
        CancellationToken cancellationToken)
    {
        return null;
    }
}