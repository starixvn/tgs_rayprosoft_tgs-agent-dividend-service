using MediatR;
using Tgs.Agent.Dividend.Core.Workers;
using Tgs.Agent.Dividend.Domain.Interfaces.Repositories;
using Tgs.Agent.Dividend.Domain.Models;

namespace Tgs.Agent.Dividend.Core;

internal class GenerateAgentDividendQueueHandler(IAgentDividendTaskQueue queue,
        IRepository repository)
    : IRequestHandler<GenerateAgentDividendRequest>
{
    public Task Handle(GenerateAgentDividendRequest request, CancellationToken cancellationToken)
    {
        return repository.GetNonArchivedBrandIdsAsyncEnumerable(request.BrandId)
            .ForEachAwaitAsync(async brandId =>
                {
                    await queue.QueueAgentDividendAsync(new AgentDividendQueueModel(brandId));
                },
                cancellationToken);
    }
}