using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Tgs.Agent.Dividend.Domain;
using Tgs.Agent.Dividend.Domain.Enums;
using Tgs.Agent.Dividend.Domain.Events;
using Tgs.Agent.Dividend.Domain.Interfaces;
using Tgs.Agent.Dividend.Domain.Interfaces.Repositories;
using Tgs.Agent.Dividend.Domain.Models;

namespace Tgs.Agent.Dividend.Core;

internal class AgentDividendHandler(
    IRepository repository,
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

        var maxId = await repository.GetMaxAgentDividendIdAsync(cancellationToken).ConfigureAwait(false);

        var agents = repository.GetAgentsCacheAsyncEnumerable(request.BrandId, cancellationToken);

        var currentNumber = maxId;

        var agentPlayerAggregates = agents.SelectManyAwait(async (x, i) =>
        {
            var agentContract = contracts.FirstOrDefault(a =>
                a.AgentId == x.Id && a.ContractStatus == (short)ContractStatus.Enabled);

            var distributionService =
                serviceProvider.GetRequiredKeyedService<IDistribution>(
                    ((ContractDividendType)agentContract.ContractDividendType).ToString());

            var data = distributionService.GetAgentPlayerAggregateAsync(currentNumber + i,
                brand,
                agentContract,
                cancellationToken);

            currentNumber += await data.CountAsync(cancellationToken);

            return data;
        });

        var multipleLayers = agentPlayerAggregates.SelectMany(x =>
        {
            var agentContract = contracts.FirstOrDefault(a =>
                a.AgentId == x.AgentId && a.ContractStatus == (short)ContractStatus.Enabled);

            var distributionService =
                serviceProvider.GetRequiredKeyedService<IDistribution>(
                    ((ContractDividendType)agentContract.ContractDividendType).ToString());

            return distributionService.GenerateMultipleLayerAsync(
                brand,
                agentContract,
                x,
                cancellationToken);
        });

        var additional = multipleLayers.SelectMany(x =>
        {
            var agentContract = contracts.FirstOrDefault(a =>
                a.AgentId == x.DividendProviderAgentId && a.ContractStatus == (short)ContractStatus.Enabled);

            var distributionService =
                serviceProvider.GetRequiredKeyedService<IDistribution>(
                    ((ContractDividendType)agentContract.ContractDividendType).ToString());

            return distributionService.GenerateAdditionalAgentPlayerAsync(currentNumber,
                brand,
                x,
                agentPlayerAggregates,
                cancellationToken);
        });

        var agentDividends = agentPlayerAggregates.Concat(additional)
            .SelectAwait(async x =>
            {
                var agentContract = contracts.FirstOrDefault(a =>
                    a.AgentId == x.AgentId && a.ContractStatus == (short)ContractStatus.Enabled);

                var distributionService =
                    serviceProvider.GetRequiredKeyedService<IDistribution>(
                        ((ContractDividendType)agentContract.ContractDividendType).ToString());

                var parentAgentDividends = distributionService.GetMultipleLayer(x,
                    await multipleLayers.ToArrayAsync(cancellationToken),
                    cancellationToken);

                x.AgentDividendAmount =
                    distributionService.GetAgentDividendAmount(x,
                        await multipleLayers.Where(m => m.DividendProviderId == x.Id).ToArrayAsync(cancellationToken),
                        cancellationToken);

                return new CreateAgentDividendModel
                {
                    Id = x.Id,
                    BrandId = x.BrandId,
                    AgentId = x.AgentId,
                    From = x.From.UtcDateTime,
                    To = x.To.UtcDateTime,
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
                    WinLossAmount = x.WinLossAmount,
                    NoIncludeNetProfitCompanyAdjustAmount = x.NoIncludeNetProfitCompanyAdjustAmount,
                    NoIncludeNetProfitAgentAdjustAmount = x.NoIncludeNetProfitAgentAdjustAmount,
                    NoIncludeNetProfitBonusAdjustAmount = x.NoIncludeNetProfitBonusAdjustAmount,
                    NetProfit = x.NetProfit(brand.PlatformCharge),
                    AgentDividendAmount = x.AgentDividendAmount,
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
                                Dividend = s.Dividend
                            })
                        .ToArray()
                };
            });

        var sendDividend =
            await mediator.Send(new GetNoSendDividendRequest(agentDividends.ToEnumerable()), cancellationToken);

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

        foreach (var createAgentDividendModel in sendDividend.Model.Where(x => x.AutoSend))
            await mediator.Publish(new SendAgentDividendEvent(brand.Id,
                    createAgentDividendModel.AgentId,
                    createAgentDividendModel.WalletId,
                    createAgentDividendModel.AgentDividendAmount,
                    createAgentDividendModel.Id),
                cancellationToken);

        repository.RemoveAllMemoryCacheAsync(cancellationToken);
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