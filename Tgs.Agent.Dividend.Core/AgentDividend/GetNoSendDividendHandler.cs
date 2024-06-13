using MediatR;
using Tgs.Agent.Dividend.Domain.Interfaces.Repositories;
using Tgs.Agent.Dividend.Domain.Models;

namespace Tgs.Agent.Dividend.Core.AgentDividend;

internal class GetNoSendDividendHandler(IRepository repository)
    : IRequestHandler<GetNoSendDividendRequest, NoSendDividendResponse>
{
    public async Task<NoSendDividendResponse> Handle(GetNoSendDividendRequest request,
        CancellationToken cancellationToken)
    {
        var deductedNegativeDividends = new Dictionary<Guid, decimal>();
        var noSendDividendIds = new Dictionary<int, int>();

        foreach (var dividend in request.AgentDividends)
        {
            var agentNoSendDividends = await repository.GetNegativeAgentDividendsAsync(dividend.AgentId)
                .ToArrayAsync(cancellationToken)
                .ConfigureAwait(false);

            var agentNoSendDividend =
                CalculateAgentNoSendDividend(dividend, deductedNegativeDividends, agentNoSendDividends);

            dividend.NegativeAgentDividendAmount = agentNoSendDividend;

            DeductNegativeDividend(dividend, agentNoSendDividend, deductedNegativeDividends, request.AgentDividends);

            if (agentNoSendDividend < 0 && dividend.AgentDividendAmount + agentNoSendDividend >= 0)
            {
                AdjustDividendAmount(dividend, agentNoSendDividend);
                UpdateNoSendDividendIds(agentNoSendDividends, noSendDividendIds, dividend, request.AgentDividends);
            }

            HandleAutoSend(dividend, agentNoSendDividend);
        }

        return new NoSendDividendResponse { Model = request.AgentDividends, NegativeDividendsIds = noSendDividendIds };
    }

    private static decimal CalculateAgentNoSendDividend(AgentDividendModel dividend,
        Dictionary<Guid, decimal> deductedNegativeDividends,
        IEnumerable<NegativeAgentDividendModel> agentNoSendDividends)
    {
        var negativeAmount = agentNoSendDividends.Sum(x => x.AgentDividendAmount);
        negativeAmount = Math.Min(0M, negativeAmount);
        var haveDeductedNegativeDividend =
            deductedNegativeDividends.TryGetValue(dividend.AgentId, out var deductedNegativeDividend);
        return haveDeductedNegativeDividend ? deductedNegativeDividend : negativeAmount;
    }

    private static void DeductNegativeDividend(AgentDividendModel dividend,
        decimal agentNoSendDividend,
        Dictionary<Guid, decimal> deductedNegativeDividends,
        IEnumerable<AgentDividendModel> agentDividendModels)
    {
        var agentDividendCount = agentDividendModels.Count(x => x.AgentId == dividend.AgentId);

        if (agentDividendCount > 1)
        {
            var actualAmount = dividend.AgentDividendAmount + agentNoSendDividend;

            var changeToPositiveNumber = actualAmount >= 0;

            deductedNegativeDividends[dividend.AgentId] = changeToPositiveNumber ? 0 : actualAmount;

            if (!changeToPositiveNumber) dividend.ShouldReferToNextRecord = true;
        }
    }

    private static void UpdateNoSendDividendIds(IEnumerable<NegativeAgentDividendModel> agentNoSendDividends,
        Dictionary<int, int> noSendDividendIds,
        AgentDividendModel dividend,
        IEnumerable<AgentDividendModel> agentDividendModels)
    {
        foreach (var negativeDividend in agentNoSendDividends.Where(nd => !noSendDividendIds.ContainsKey(nd.Id)))
            noSendDividendIds.Add(negativeDividend.Id, dividend.Id);

        foreach (var agentDividendModel in agentDividendModels.Where(x =>
                     x.AgentId == dividend.AgentId && x.ShouldReferToNextRecord))
        {
            agentDividendModel.ReferencedAgentDividendId = dividend.Id;
            agentDividendModel.ShouldReferToNextRecord = false;
        }
    }

    private static void AdjustDividendAmount(AgentDividendModel dividend, decimal agentNoSendDividend)
    {
        dividend.BeforeDividendAmount = dividend.AgentDividendAmount;
        dividend.AgentDividendAmount = Math.Max(0, dividend.AgentDividendAmount + agentNoSendDividend);
    }

    private static void HandleAutoSend(AgentDividendModel dividend, decimal agentNoSendDividend)
    {
        if ((dividend.BeforeDividendAmount ?? dividend.AgentDividendAmount) + agentNoSendDividend < 0)
            dividend.AutoSend = false;
    }
}