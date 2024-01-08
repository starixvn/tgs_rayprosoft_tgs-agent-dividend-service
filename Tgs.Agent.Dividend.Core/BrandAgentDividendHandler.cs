using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Tgs.Agent.Dividend.Domain;
using Tgs.Agent.Dividend.Domain.Enums;
using Tgs.Agent.Dividend.Domain.Events;
using Tgs.Agent.Dividend.Domain.Interfaces;
using Tgs.Agent.Dividend.Domain.Interfaces.Repositories;
using Tgs.Agent.Dividend.Domain.Models;

namespace Tgs.Agent.Dividend.Core;

internal class BrandAgentDividendHandler(
    IRepository repository,
    IContractRepository contractRepository,
    IMediator mediator,
    IServiceProvider serviceProvider,
    ILogger<BrandAgentDividendHandler> logger)
    : IRequestHandler<AgentDividendRequest>
{
    public async Task Handle(AgentDividendRequest request, CancellationToken cancellationToken)
    {
        var contracts = await contractRepository.GetBrandAgentDividendContractsAsync(request.BrandId)
            .ToArrayAsync(cancellationToken);

        var brand = await repository.GetBrandAsync(request.BrandId, cancellationToken).ConfigureAwait(false);

        var maxId = await repository.GetMaxAgentDividendIdAsync(cancellationToken).ConfigureAwait(false);

        var agentInstances = await repository.GetAgentsCacheAsyncEnumerable(request.BrandId, cancellationToken)
            .Select(x =>
            {
                var agentContract = contracts.FirstOrDefault(a =>
                                        a.AgentId == x.Id &&
                                        a.ContractStatus == (short)ContractStatus.Enabled) ??
                                    throw new Exception(
                                        $"Can't find contract agentId: {x.Id}");

                var distributionService =
                    serviceProvider.GetRequiredKeyedService<IDistribution>(
                        ((ContractDividendType)agentContract.ContractDividendType).ToString());

                return (agentContract, distributionService);
            })
            .ToArrayAsync(cancellationToken);

        var currentNumber = maxId;

        var agentPlayerAggregates = new List<AgentPlayerAggregateModel>();

        foreach (var (agentContract, distributionService) in agentInstances)
        {
            var data = (await distributionService.GetAgentPlayerAggregateAsync(currentNumber,
                    brand,
                    agentContract,
                    cancellationToken)
                .ToArrayAsync(cancellationToken)).ToArray();

            currentNumber += data.Length;

            agentPlayerAggregates.AddRange(data);
        }

        var multipleLayers = new List<AgentDividendMultipleLayerModel>();

        foreach (var agentPlayerAggregateModel in agentPlayerAggregates)
        {
            var (agentContract, distributionService) = agentInstances.First(a => a.agentContract.AgentId == agentPlayerAggregateModel.AgentId);

            var data = await distributionService.GenerateMultipleLayerAsync(
                    brand,
                    agentContract,
                    agentPlayerAggregateModel,
                    cancellationToken)
                .ToArrayAsync(cancellationToken);

            multipleLayers.AddRange(data);
        }

        var additional = new List<AgentPlayerAggregateModel>();

        foreach (var data in from agentDividendMultipleLayerModel in multipleLayers
                 let instance =
                     agentInstances.First(a => a.agentContract.AgentId == agentDividendMultipleLayerModel.AgentId)
                 select instance.distributionService.GenerateAdditionalAgentPlayerAggregates(currentNumber,
                         brand,
                         agentDividendMultipleLayerModel,
                         agentPlayerAggregates,
                         cancellationToken)
                     .ToArray())
        {
            currentNumber += data.Length;

            additional.AddRange(data);
        }

        var agentDividends = agentPlayerAggregates.Concat(additional)
            .Select(x =>
            {
                var (agentContract, distributionService) = agentInstances.First(a => a.agentContract.AgentId == x.AgentId);

                var parentAgentDividends = distributionService.GetMultipleLayer(x,
                    multipleLayers,
                    cancellationToken);

                x.AgentDividendAmount =
                    distributionService.GetAgentDividendAmount(x,
                        multipleLayers.Where(m => m.DividendProviderId == x.Id),
                        cancellationToken);

                return new CreateAgentDividendModel
                {
                    Id = x.Id,
                    BrandId = x.BrandId,
                    AgentId = x.AgentId,
                    From = x.From.UtcDateTime,
                    To = x.To.UtcDateTime,
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
            await mediator.Send(new GetNoSendDividendRequest(agentDividends), cancellationToken);

        var (nextFrom, nextTo) = DateTimeOffsetExtensions.GetInterval(
            (SettlementIntervalType)brand.SettlementIntervalType,
            new DateTimeOffset(brand.NextSettlementTo).AddDays(1).ToOffset(new TimeSpan(8, 0, 0)),
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

        repository.RemoveAllMemoryCache(cancellationToken);
    }
}