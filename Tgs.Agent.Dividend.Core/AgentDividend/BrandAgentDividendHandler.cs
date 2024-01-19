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
    IContractRepository contractRepository,
    IMediator mediator,
    IServiceProvider serviceProvider,
    ILogger<BrandAgentDividendHandler> logger)
    : IRequestHandler<AgentDividendRequest, NoSendDividendResponse>
{
    public async Task<NoSendDividendResponse> Handle(AgentDividendRequest request, CancellationToken cancellationToken)
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
                    serviceProvider.GetRequiredKeyedService<IDividendCalculator>(
                        ((ContractDividendType)agentContract.ContractDividendType).ToString());

                return (agentContract, distributionService);
            })
            .ToArrayAsync(cancellationToken);

        var currentNumber = maxId;

        var agentPlayerAggregates = new List<AgentPlayerAggregateModel>();

        foreach (var (agentContract, distributionService) in agentInstances)
        {
            var data = await distributionService.GetAgentPlayerAggregateAsync(currentNumber,
                    brand,
                    agentContract,
                    cancellationToken)
                .ToArrayAsync(cancellationToken);

            currentNumber += data.Length;

            agentPlayerAggregates.AddRange(data);
        }

        var multipleLayers = new List<AgentDividendMultipleLayerModel>();

        foreach (var agentPlayerAggregateModel in agentPlayerAggregates)
        {
            var (agentContract, distributionService) =
                agentInstances.First(a => a.agentContract.AgentId == agentPlayerAggregateModel.AgentId);

            var data = await distributionService.GenerateMultipleLayerAsync(
                    brand,
                    agentContract,
                    agentPlayerAggregateModel,
                    cancellationToken)
                .ToArrayAsync(cancellationToken);

            multipleLayers.AddRange(data);
        }

        var agentDividends = agentPlayerAggregates
            .Select(x =>
            {
                var (agentContract, distributionService) =
                    agentInstances.First(a => a.agentContract.AgentId == x.AgentId);

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
                                Dividend = s.Dividend,
                                AgentDividendSnapshotId = s.AgentDividendSnapshotId
                            })
                        .ToArray()
                };
            });

        var sendDividend =
            await mediator.Send(new GetNoSendDividendRequest(agentDividends.ToArray()), cancellationToken);

        repository.RemoveAllMemoryCache(cancellationToken);

        return sendDividend;
    }
}