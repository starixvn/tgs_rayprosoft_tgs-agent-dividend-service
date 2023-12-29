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

internal class AgentDividendHandler(
    IRepository repository,
    IContractRepository contractRepository,
    IMediator mediator,
    IServiceProvider serviceProvider,
    ILogger<AgentDividendHandler> logger)
    : IRequestHandler<AgentDividendRequest>
{
    public async Task Handle(AgentDividendRequest request, CancellationToken cancellationToken)
    {
        var contracts = await contractRepository.GetAgentDividendContractsAsync(request.BrandId)
            .ToArrayAsync(cancellationToken);

        var brand = await repository.GetBrandAsync(request.BrandId, cancellationToken).ConfigureAwait(false);

        var maxId = await repository.GetMaxAgentDividendIdAsync(cancellationToken).ConfigureAwait(false);

        var agents = await repository.GetAgentsCacheAsyncEnumerable(request.BrandId, cancellationToken)
            .ToArrayAsync(cancellationToken);

        var currentNumber = maxId;

        var agentPlayerAggregates = new List<AgentPlayerAggregateModel>();

        var i = 0;

        foreach (var agentModel in agents)
        {
            var data = (await GetAgentPlayerAggregatesAsync(currentNumber + i,
                contracts,
                agentModel,
                brand,
                cancellationToken)).ToArray();

            currentNumber += data.Length;

            agentPlayerAggregates.AddRange(data);
        }

        var multipleLayers = new List<AgentDividendMultipleLayerModel>();

        foreach (var agentPlayerAggregateModel in agentPlayerAggregates)
        {
            var agentContract = contracts.FirstOrDefault(a =>
                                    a.AgentId == agentPlayerAggregateModel.AgentId &&
                                    a.ContractStatus == (short)ContractStatus.Enabled) ??
                                throw new Exception(
                                    $"Can't find contract agentId: {agentPlayerAggregateModel.AgentId}");

            var distributionService =
                serviceProvider.GetRequiredKeyedService<IDistribution>(
                    ((ContractDividendType)agentContract.ContractDividendType).ToString());

            var data = await distributionService.GenerateMultipleLayerAsync(
                    brand,
                    agentContract,
                    agentPlayerAggregateModel,
                    cancellationToken)
                .ToArrayAsync(cancellationToken);

            multipleLayers.AddRange(data);
        }

        var additional = new List<AgentPlayerAggregateModel>();

        foreach (var agentDividendMultipleLayerModel in multipleLayers)
        {
            var agentContract = contracts.FirstOrDefault(a =>
                                    a.AgentId == agentDividendMultipleLayerModel.DividendProviderAgentId &&
                                    a.ContractStatus == (short)ContractStatus.Enabled) ??
                                throw new Exception(
                                    $"Can't find contract agentId: {agentDividendMultipleLayerModel.AgentId}");
            ;

            var distributionService =
                serviceProvider.GetRequiredKeyedService<IDistribution>(
                    ((ContractDividendType)agentContract.ContractDividendType).ToString());

            var data = distributionService.GenerateAdditionalAgentPlayerAggregates(currentNumber,
                    brand,
                    agentDividendMultipleLayerModel,
                    agentPlayerAggregates,
                    cancellationToken)
                .ToArray();

            additional.AddRange(data);
        }

        var agentDividends = agentPlayerAggregates.Concat(additional)
            .Select(x =>
            {
                var agentContract = contracts.FirstOrDefault(a =>
                                        a.AgentId == x.AgentId && a.ContractStatus == (short)ContractStatus.Enabled) ??
                                    throw new Exception($"Can't find contract agentId: {x.AgentId}");
                ;

                var distributionService =
                    serviceProvider.GetRequiredKeyedService<IDistribution>(
                        ((ContractDividendType)agentContract.ContractDividendType).ToString());

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

        repository.RemoveAllMemoryCache(cancellationToken);
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

    private async Task<IEnumerable<AgentPlayerAggregateModel>> GetAgentPlayerAggregatesAsync(int number,
        IEnumerable<AgentDividendContractModel> contracts,
        AgentModel agent,
        BrandModel brand,
        CancellationToken cancellationToken)
    {
        var agentContract = contracts.FirstOrDefault(a =>
                                a.AgentId == agent.Id && a.ContractStatus == (short)ContractStatus.Enabled) ??
                            throw new Exception($"Can't find contract agentId: {agent.Id}");

        var distributionService =
            serviceProvider.GetRequiredKeyedService<IDistribution>(
                ((ContractDividendType)agentContract.ContractDividendType).ToString());

        return await distributionService.GetAgentPlayerAggregateAsync(number,
                brand,
                agentContract,
                cancellationToken)
            .ToArrayAsync(cancellationToken);
    }
}