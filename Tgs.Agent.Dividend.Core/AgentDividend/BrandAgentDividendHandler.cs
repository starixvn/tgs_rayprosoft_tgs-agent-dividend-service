using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Tgs.Agent.Dividend.Domain.Enums;
using Tgs.Agent.Dividend.Domain.Interfaces;
using Tgs.Agent.Dividend.Domain.Interfaces.Repositories;
using Tgs.Agent.Dividend.Domain.Models;

namespace Tgs.Agent.Dividend.Core.AgentDividend;

internal class BrandAgentDividendHandler(
    IRepository repository,
    IMediator mediator,
    IServiceProvider serviceProvider)
    : IRequestHandler<AgentDividendRequest, NoSendDividendResponse>
{
    public async Task<NoSendDividendResponse> Handle(AgentDividendRequest request, CancellationToken cancellationToken)
    {
        var brand = await repository.GetBrandAsync(request.BrandId, cancellationToken).ConfigureAwait(false);

        var maxId = await repository.GetMaxAgentDividendIdAsync(cancellationToken).ConfigureAwait(false);

        var agentInstances = await repository.GetAgentsCacheAsyncEnumerable(request.BrandId, cancellationToken)
            .Select(x =>
            {
                var distributionService =
                    serviceProvider.GetRequiredKeyedService<IDividendCalculator>(
                        ((AgentDividendType)brand.AgentDividendType).ToString());

                return (agent: x, distributionService);
            })
            .ToArrayAsync(cancellationToken);

        var currentNumber = maxId;

        var agentPlayerAggregates = new List<AgentPlayerAggregateModel>();

        foreach (var (agent, distributionService) in agentInstances)
        {
            var data = await distributionService.GetAgentPlayerAggregateAsync(currentNumber,
                    brand,
                    agent,
                    cancellationToken)
                .ToArrayAsync(cancellationToken);

            currentNumber += data.Length;

            agentPlayerAggregates.AddRange(data);
        }

        var multipleLayers = new List<AgentDividendMultipleLayerModel>();

        foreach (var agentPlayerAggregateModel in agentPlayerAggregates)
        {
            var (agent, distributionService) =
                agentInstances.First(a => a.agent.Id == agentPlayerAggregateModel.AgentId);

            var data = await distributionService.GenerateMultipleLayerAsync(
                    brand,
                    agent,
                    agentPlayerAggregateModel,
                    request.PreCalculation,
                    cancellationToken)
                .ToArrayAsync(cancellationToken);

            multipleLayers.AddRange(data);
        }

        var agentDividends = agentPlayerAggregates
            .Select(x =>
            {
                var (agent, distributionService) =
                    agentInstances.First(a => a.agent.Id == x.AgentId);

                var parentAgentDividends = distributionService.GetMultipleLayer(x,
                    multipleLayers,
                    cancellationToken);

                x.AgentDividendAmount =
                    distributionService.GetAgentDividendAmount(x,
                        multipleLayers,
                        cancellationToken);

                return new AgentDividendModel
                {
                    Id = x.Id,
                    BrandId = x.BrandId,
                    AgentId = x.AgentId,
                    From = x.From.UtcDateTime,
                    To = x.To.UtcDateTime,
                    SentOn = !agent.NegativeAgentDividendCarryForward && x.AgentDividendAmount <= 0 ? DateTimeOffset.UtcNow : null,
                    AutoSend = brand.AutoSentAgentBonus,
                    AgentDividendPercentage = x.AgentDividendPercentage,
                    PromotionAmount = x.PromotionAmount,
                    RebateAmount = x.RebateAmount,
                    DepositAmount = x.DepositAmount,
                    WithdrawalAmount = x.WithdrawalAmount,
                    CompanyAdjustAmount = x.CompanyAdjustAmount,
                    AgentAdjustAmount = x.AgentAdjustAmount,
                    BonusAdjustAmount = x.BonusAdjustAmount,
                    BonusAmount = x.BonusAmount,
                    BetAmount = x.BetAmount,
                    ValidBetAmount = x.ValidBetAmount,
                    OriginalTurnover = x.OriginalTurnover,
                    Turnover = x.Turnover,
                    LiveDealerWinLoss = x.LiveDealerWinLoss,
                    SlotWinLoss = x.SlotWinLoss,
                    FishingWinLoss = x.FishingWinLoss,
                    ArcadeWinLoss = x.ArcadeWinLoss,
                    TableGameWinLoss = x.TableGameWinLoss,
                    LotteryWinLoss = x.LotteryWinLoss,
                    CardGameWinLoss = x.CardGameWinLoss,
                    SportsWinLoss = x.SportsWinLoss,
                    ESportsWinLoss = x.ESportsWinLoss,
                    VirtualSportsWinLoss = x.VirtualSportsWinLoss,
                    WinLossAmount = x.WinLossAmount,
                    NoIncludeNetProfitCompanyAdjustAmount = x.NoIncludeNetProfitCompanyAdjustAmount,
                    NoIncludeNetProfitAgentAdjustAmount = x.NoIncludeNetProfitAgentAdjustAmount,
                    NoIncludeNetProfitBonusAdjustAmount = x.NoIncludeNetProfitBonusAdjustAmount,
                    NetProfit = x.NetProfit(),
                    AgentDividendAmount = x.AgentDividendAmount,
                    ActiveNumber = x.ActiveNumber,
                    NegativeAgentDividendCarryForward = agent.NegativeAgentDividendCarryForward,
                    AgentDividendMultipleLayerModels = parentAgentDividends.Select(s
                            => new AgentDividendMultipleLayerModel
                            {
                                AgentId = s.AgentId,
                                AgentDividendId = x.Id,
                                AgentDividendPercentage = s.AgentDividendPercentage,
                                ActualDividendPercentage = s.ActualDividendPercentage,
                                AgentCoveredBonus = s.AgentCoveredBonus,
                                AgentCoveredBonusAdjust = s.AgentCoveredBonusAdjust,
                                DividendProviderAgentId = s.DividendProviderAgentId,
                                DividendProviderId = s.DividendProviderId,
                                Layer = s.Layer,
                                Dividend = s.Dividend,
                                AgentDividendSnapshotId = s.AgentDividendSnapshotId
                            })
                        .ToArray(),
                    AgentDividendPlatformCharges = x.AgentDividendPlatformCharges ?? [],
                    AgentDividendProductFees = x.AgentDividendProductFees ?? []
                };
            });

        var sendDividend =
            await mediator.Send(new GetNoSendDividendRequest(agentDividends.ToArray()), cancellationToken);

        repository.RemoveAllMemoryCache(cancellationToken);

        return sendDividend;
    }
}