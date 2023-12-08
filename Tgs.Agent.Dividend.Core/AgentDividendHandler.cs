using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Tgs.Agent.Dividend.Domain;
using Tgs.Agent.Dividend.Domain.Enums;
using Tgs.Agent.Dividend.Domain.Events;
using Tgs.Agent.Dividend.Domain.Interfaces;
using Tgs.Agent.Dividend.Domain.Interfaces.Repositories;
using Tgs.Agent.Dividend.Domain.Models;

namespace Tgs.Agent.Dividend.Core;

internal class AgentDividendHandler(IRepository repository,
        IContractRepository contractRepository,
        IMediator mediator,
        IServiceProvider serviceProvider)
    : IRequestHandler<AgentDividendRequest>
{
    public async Task Handle(AgentDividendRequest request, CancellationToken cancellationToken)
    {
        var contracts = await contractRepository.GetAgentDividendContractsAsync(request.BrandId)
            .ToArrayAsync(cancellationToken);

        var brand = await repository.GetBrandAsync(request.BrandId, cancellationToken).ConfigureAwait(false);

        var from = brand.CurrentSettlementFrom;

        var to = brand.CurrentSettlementTo;

        var maxId = await repository.GetMaxAgentDividendIdAsync(cancellationToken).ConfigureAwait(false);

        var agents = await repository.GetAgentsCacheAsyncEnumerable(request.BrandId, cancellationToken)
            .ToArrayAsync(cancellationToken);

        var allLayers =
            new List<(AgentPlayerAggregateModel agentData, AgentDividendMultipleLayerModel[] multipleLayer)>();

        var i = 1;

        foreach (var x in agents)
        {
            var agentContract = contracts.FirstOrDefault(a =>
                a.AgentId == x.Id && a.ContractStatus == (short)ContractStatus.Enabled);

            var distributionService =
                serviceProvider.GetRequiredKeyedService<IDistribution>(
                    ((ContractDividendType)agentContract.ContractDividendType).ToString());

            var dividends = await distributionService.AgentDividendGenerationAsync(maxId + i,
                    brand,
                    agentContract,
                    cancellationToken)
                .ToArrayAsync(cancellationToken);

            i = dividends.Length + i;

            allLayers.AddRange(
                dividends.Select(dividend => (dividend.agentPlayerAggregate, dividend.multipleLayer)));
        }

        var all = allLayers.SelectMany(x => x.multipleLayer);

        var agentDividends = allLayers.Select(x =>
        {
            var agentContract = contracts.FirstOrDefault(a =>
                a.AgentId == x.agentData.AgentId && a.ContractStatus == (short)ContractStatus.Enabled);

            var distributionService =
                serviceProvider.GetRequiredKeyedService<IDistribution>(
                    ((ContractDividendType)agentContract.ContractDividendType).ToString());

            var parentAgentDividends = distributionService.GetMultipleLayer(x.agentData, all, cancellationToken);

            x.agentData.AgentDividendAmount =
                distributionService.GetAgentDividendAmount(x.agentData, x.multipleLayer, cancellationToken);

            return new CreateAgentDividendModel
            {
                Id = x.agentData.Id,
                BrandId = x.agentData.BrandId,
                AgentId = x.agentData.AgentId,
                From = x.agentData.From.UtcDateTime,
                To = x.agentData.To.UtcDateTime,
                AgentDividendPercentage = x.agentData.AgentDividendPercentage,
                PromotionAmount = x.agentData.PromotionAmount,
                RebateAmount = x.agentData.RebateAmount,
                DepositAmount = x.agentData.DepositAmount,
                WithdrawalAmount = x.agentData.WithdrawalAmount,
                CompanyAdjustAmount = x.agentData.CompanyAdjustAmount,
                AgentAdjustAmount = x.agentData.AgentAdjustAmount,
                BonusAdjustAmount = x.agentData.BonusAdjustAmount,
                BonusAmount = x.agentData.BonusAmount,
                BetAmount = x.agentData.BetAmount,
                ValidBetAmount = x.agentData.ValidBetAmount,
                OriginalTurnover = x.agentData.OriginalTurnover,
                Turnover = x.agentData.Turnover,
                WinLossAmount = x.agentData.WinLossAmount,
                NoIncludeNetProfitCompanyAdjustAmount = x.agentData.NoIncludeNetProfitCompanyAdjustAmount,
                NoIncludeNetProfitAgentAdjustAmount = x.agentData.NoIncludeNetProfitAgentAdjustAmount,
                NoIncludeNetProfitBonusAdjustAmount = x.agentData.NoIncludeNetProfitBonusAdjustAmount,
                NetProfit = x.agentData.NetProfit(brand.PlatformCharge),
                AgentDividendAmount = x.agentData.AgentDividendAmount,
                AgentDividendMultipleLayerModels = parentAgentDividends.Select(s
                        => new AgentDividendMultipleLayerModel
                        {
                            AgentId = s.AgentId,
                            AgentDividendId = x.agentData.Id,
                            AgentDividendPercentage = s.AgentDividendPercentage,
                            ActualDividendPercentage = s.ActualDividendPercentage,
                            AgentCoveredBonus = s.AgentCoveredBonus,
                            AgentCoveredBonusAdjust = s.AgentCoveredBonusAdjust,
                            DividendProviderAgentId = s.DividendProviderAgentId,
                            DividendProviderId = s.DividendProviderId,
                            Layer = s.Layer,
                            Dividend = s.Dividend
                        })
                    .ToArray()
            };
        });

        var agentPlayerGameAggregates =
            repository.GetAgentPlayerGameAggregatesCacheAsync(brand.Id,
                from,
                to);

        foreach (var agentDividend in agentDividends)
        {
            var gameAggregates =
                await agentPlayerGameAggregates.FirstOrDefaultAsync(x => x.AgentId == agentDividend.AgentId,
                    cancellationToken);

            agentDividend.LiveDealerWinLoss = gameAggregates?.LiveDealerWinLoss ?? 0M;
            agentDividend.SlotWinLoss = gameAggregates?.SlotWinLoss ?? 0M;
            agentDividend.FishingWinLoss = gameAggregates?.FishingWinLoss ?? 0M;
            agentDividend.ArcadeWinLoss = gameAggregates?.ArcadeWinLoss ?? 0M;
            agentDividend.TableGameWinLoss = gameAggregates?.TableGameWinLoss ?? 0M;
            agentDividend.LotteryWinLoss = gameAggregates?.LotteryWinLoss ?? 0M;
            agentDividend.CardGameWinLoss = gameAggregates?.CardGameWinLoss ?? 0M;
            agentDividend.SportsWinLoss = gameAggregates?.SportsWinLoss ?? 0M;
            agentDividend.ESportsWinLoss = gameAggregates?.ESportsWinLoss ?? 0M;
            agentDividend.VirtualSportsWinLoss = gameAggregates?.VirtualSportsWinLoss ?? 0M;
        }

        var sendDividend = await mediator.Send(new GetNoSendDividendRequest(agentDividends), cancellationToken);

        var (nextFrom, nextTo) = GetInterval(
            (SettlementIntervalType)brand.SettlementIntervalType,
            new DateTimeOffset(brand.NextSettlementTo).ToOffset(new TimeSpan(8, 0, 0)),
            brand.SettlementDay);

        brand.PreviousSettlementFrom = brand.CurrentSettlementFrom;
        brand.PreviousSettlementTo = brand.CurrentSettlementTo;
        brand.CurrentSettlementFrom = brand.NextSettlementFrom;
        brand.CurrentSettlementTo = brand.NextSettlementTo;
        brand.NextSettlementFrom = nextFrom.UtcDateTime;
        brand.NextSettlementTo = nextTo.UtcDateTime;

        await repository.CreateAgentDividendsAsync(brand,
            sendDividend.Model,
            sendDividend.NegativeDividendsIds,
            cancellationToken);

        foreach (var x in agentDividends)
            await mediator.Publish(new SendAgentDividendEvent(brand.Id,
                    x.AgentId,
                    x.WalletId,
                    x.AgentDividendAmount,
                    x.Id),
                cancellationToken);
    }

    private static (DateTimeOffset, DateTimeOffset) GetInterval(SettlementIntervalType type,
        DateTimeOffset startDate,
        int? dayOfMonth)
    {
        var date = startDate.AddDays(1);

        return type switch
        {
            SettlementIntervalType.Daily => (date, date),
            SettlementIntervalType.Weekly => (date.GetMonday(),
                date.GetSunday()),
            SettlementIntervalType.Monthly => (date,
                dayOfMonth == null
                    ? date.GetLastDayOfMonth()
                    : startDate.AddMonths(1).GetSpecifyDayOfMonth(dayOfMonth.Value)),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }
}