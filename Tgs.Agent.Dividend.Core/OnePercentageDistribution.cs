using System.Runtime.CompilerServices;
using Tgs.Agent.Dividend.Domain;
using Tgs.Agent.Dividend.Domain.Interfaces;
using Tgs.Agent.Dividend.Domain.Interfaces.Repositories;
using Tgs.Agent.Dividend.Domain.Models;

namespace Tgs.Agent.Dividend.Core;

internal class OnePercentageDistribution
    (IContractRepository contractRepository, IRepository repository) : IDistribution
{
    public async
        IAsyncEnumerable<(AgentPlayerAggregateModel agentPlayerAggregate, AgentDividendMultipleLayerModel[]
            multipleLayer)> AgentDividendGenerationAsync(int idNumber,
            BrandModel brand,
            AgentDividendContractModel contract,
            [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var agentAggregate = await repository
            .GetAgentPlayerAggregatesForMultipleLayerCacheAsync(brand.Id,
                brand.CurrentSettlementFrom,
                brand.CurrentSettlementTo,
                cancellationToken)
            .Where(x => x.AgentId == contract.AgentId)
            .FirstOrDefaultAsync(cancellationToken);

        var agentPlayerGameAggregate =
            await repository.GetAgentPlayerGameAggregatesCacheAsync(brand.Id,
                    brand.CurrentSettlementFrom,
                    brand.CurrentSettlementTo)
                .FirstOrDefaultAsync(x => x.AgentId == contract.AgentId, cancellationToken);

        agentAggregate ??= GetDefaultAggregate(brand, contract.AgentId);

        agentAggregate.LiveDealerWinLoss = agentPlayerGameAggregate?.LiveDealerWinLoss ?? 0M;
        agentAggregate.SlotWinLoss = agentPlayerGameAggregate?.SlotWinLoss ?? 0M;
        agentAggregate.FishingWinLoss = agentPlayerGameAggregate?.FishingWinLoss ?? 0M;
        agentAggregate.ArcadeWinLoss = agentPlayerGameAggregate?.ArcadeWinLoss ?? 0M;
        agentAggregate.TableGameWinLoss = agentPlayerGameAggregate?.TableGameWinLoss ?? 0M;
        agentAggregate.LotteryWinLoss = agentPlayerGameAggregate?.LotteryWinLoss ?? 0M;
        agentAggregate.CardGameWinLoss = agentPlayerGameAggregate?.CardGameWinLoss ?? 0M;
        agentAggregate.SportsWinLoss = agentPlayerGameAggregate?.SportsWinLoss ?? 0M;
        agentAggregate.ESportsWinLoss = agentPlayerGameAggregate?.ESportsWinLoss ?? 0M;
        agentAggregate.VirtualSportsWinLoss = agentPlayerGameAggregate?.VirtualSportsWinLoss ?? 0M;

        agentAggregate.Id = idNumber;

        var agent = await repository.GetAgentsCacheAsyncEnumerable(brand.Id, cancellationToken)
            .FirstAsync(x => x.Id == contract.AgentId, cancellationToken);

        var agentLineIds = await repository.GetAgentsByPathCacheAsyncEnumerable(brand.Id, agent.Path, cancellationToken)
            .ToArrayAsync(cancellationToken);

        var from = new DateTimeOffset(brand.CurrentSettlementFrom.Year,
            brand.CurrentSettlementFrom.Month,
            brand.CurrentSettlementFrom.Day,
            0,
            0,
            0,
            TimeSpan.Zero).ToOffset(TimeSpan.FromHours(8));

        var to = new DateTimeOffset(brand.CurrentSettlementTo.Year,
            brand.CurrentSettlementTo.Month,
            brand.CurrentSettlementTo.Day,
            0,
            0,
            0,
            TimeSpan.Zero).ToOffset(TimeSpan.FromHours(8));

        var validBets = await repository
            .GetAgentPlayerValidBetsCacheAsync(brand.Id,
                brand.CurrentSettlementFrom,
                brand.CurrentSettlementTo,
                cancellationToken)
            .Where(x => x.AgentId == contract.AgentId)
            .ToArrayAsync(cancellationToken);

        var betDays = await repository
            .GetAgentPlayerBetCountsCacheAsync(brand.Id,
                brand.CurrentSettlementFrom,
                brand.CurrentSettlementTo,
                cancellationToken)
            .Where(x => x.AgentId == contract.AgentId)
            .ToArrayAsync(cancellationToken);

        var deposits = await repository
            .GetAgentPlayerDepositsCacheAsync(brand.Id,
                DateOnly.FromDateTime(from.DateTime),
                DateOnly.FromDateTime(to.DateTime),
                cancellationToken)
            .Where(x => x.AgentId == contract.AgentId)
            .ToArrayAsync(cancellationToken);

        var playerCodes = validBets.Select(x => x.PlayerCode)
            .Concat(betDays.Select(x => x.PlayerCode))
            .Concat(deposits.Select(x => x.PlayerCode))
            .Distinct();

        var conditions = await contractRepository
            .GetAgentDividendContractConditionsAsync(contract.Id, cancellationToken)
            .ToArrayAsync(cancellationToken);

        var validPlayerCount = GetValidPlayerCount(playerCodes, deposits, validBets, betDays, contract);

        var agentDividends = GetAgentDividendsByMultipleLayer(
            agentAggregate.Id,
            contract.Id,
            agent,
            brand.CheckDividendActiveCount,
            agentLineIds,
            new AgentConditionModel
            {
                ValidNumber = validPlayerCount, ValidBetAmount = validBets.Sum(s => s.ValidBetAmount),
                NetProfit = agentAggregate.NetProfit(brand.PlatformCharge)
            },
            conditions);

        yield return (agentAggregate, agentDividends.ToArray());
    }

    public decimal GetAgentDividendAmount(AgentPlayerAggregateModel agentPlayerAggregate,
        IEnumerable<AgentDividendMultipleLayerModel> multipleLayer,
        CancellationToken cancellation)
    {
        return multipleLayer.Where(p => p.AgentId == agentPlayerAggregate.AgentId)
                   .Sum(s => s.Dividend) -
               (agentPlayerAggregate.AgentAdjustAmount - agentPlayerAggregate.NoIncludeNetProfitAgentAdjustAmount);
    }

    public IEnumerable<AgentDividendMultipleLayerModel> GetMultipleLayer(AgentPlayerAggregateModel agentPlayerAggregate,
        IEnumerable<AgentDividendMultipleLayerModel> multipleLayer,
        CancellationToken cancellation)
    {
        return multipleLayer.Where(p => p.AgentId == agentPlayerAggregate.AgentId);
    }

    private static int GetValidPlayerCount(
        IEnumerable<string> playerCodes,
        IEnumerable<AgentPlayerDepositModel> deposits,
        IEnumerable<AgentPlayerValidBetModel> validBets,
        IEnumerable<AgentPlayerBetDaysModel> betDays,
        AgentDividendContractModel contract)
    {
        return playerCodes.Where(playerCode =>
            {
                var deposit = deposits.FirstOrDefault(x => x.PlayerCode == playerCode)?.DepositAmount ??
                              0M;
                var validBet = validBets.FirstOrDefault(x => x.PlayerCode == playerCode)?.ValidBetAmount ??
                               0M;
                var betDay = betDays.FirstOrDefault(x => x.PlayerCode == playerCode)?.DayCount ?? 0;

                return deposit >= contract.DepositAmount &&
                       validBet >= contract.ValidBetAmount &&
                       betDay >= contract.BetDays;
            })
            .Count();
    }

    private static IEnumerable<AgentDividendMultipleLayerModel> GetAgentDividendsByMultipleLayer(
        int id,
        Guid contractId,
        AgentModel agent,
        bool checkDividendActiveCount,
        IEnumerable<AgentModel> lineAgents,
        AgentConditionModel agentCurrentCondition,
        IEnumerable<AgentDividendContractPercentageModel> agentDividendConditions)
    {
        var (meetingTheRequirement, percentage) = GetAgentDividendPercentage(agentDividendConditions,
            agentCurrentCondition,
            checkDividendActiveCount);

        if (!meetingTheRequirement && agentCurrentCondition.NetProfit > 0)
            return lineAgents.Select(lineAgent =>
            {
                var path = lineAgent.Path.ToString();

                var pathList = path.Split(".");

                var layer = pathList.Aggregate((result, x) => $"{result},{x}");

                return new AgentDividendMultipleLayerModel
                {
                    AgentId = lineAgent.Id,
                    ContractId = contractId,
                    DividendProviderAgentId = agent.Id,
                    DividendProviderId = id,
                    AgentDividendPercentage = 0M,
                    ActualDividendPercentage = 0M,
                    Layer = layer,
                    Dividend = 0M
                };
            });

        return lineAgents.Select(lineAgent =>
        {
            var path = lineAgent.Path.ToString();

            var pathList = path.Split(".");

            var layer = pathList.Aggregate((result, x) => $"{result},{x}");

            if (agent.Id == lineAgent.Id)
                return new AgentDividendMultipleLayerModel
                {
                    AgentId = lineAgent.Id,
                    ContractId = contractId,
                    DividendProviderAgentId = agent.Id,
                    DividendProviderId = id,
                    AgentDividendPercentage = percentage,
                    ActualDividendPercentage = percentage,
                    Layer = layer,
                    Dividend = (agentCurrentCondition.NetProfit * percentage / 100M).Truncate(2)
                };

            if (agent.ParentAgentId == lineAgent.Id)
                return new AgentDividendMultipleLayerModel
                {
                    AgentId = lineAgent.Id,
                    ContractId = contractId,
                    DividendProviderAgentId = agent.Id,
                    DividendProviderId = id,
                    AgentDividendPercentage = 10,
                    ActualDividendPercentage = 10,
                    Layer = layer,
                    Dividend = (agentCurrentCondition.NetProfit * 0.1M).Truncate(2)
                };

            return new AgentDividendMultipleLayerModel
            {
                AgentId = lineAgent.Id,
                ContractId = contractId,
                DividendProviderAgentId = agent.Id,
                DividendProviderId = id,
                AgentDividendPercentage = 1,
                ActualDividendPercentage = 1,
                Layer = layer,
                Dividend = (agentCurrentCondition.NetProfit * 0.01M).Truncate(2)
            };
        });
    }

    private static (bool meetingTheRequirement, decimal percentage) GetAgentDividendPercentage(
        IEnumerable<AgentDividendContractPercentageModel> agentDividendCondition,
        AgentConditionModel currentCondition,
        bool checkDividendActiveCount)
    {
        var condition = agentDividendCondition.OrderByDescending(x => x.Rank)
            .Where(!checkDividendActiveCount,
                x => currentCondition.NetProfit >= x.NetProfit && currentCondition.ValidBetAmount >= x.WinLossAmount)
            .Where(checkDividendActiveCount,
                x => currentCondition.ValidNumber >= x.ValidNumber &&
                     currentCondition.NetProfit >= x.NetProfit &&
                     currentCondition.ValidBetAmount >= x.WinLossAmount)
            .FirstOrDefault();

        return (condition != null, condition?.Percentage ?? 0M);
    }

    private static AgentPlayerAggregateModel GetDefaultAggregate(BrandModel brand, Guid agentId)
    {
        return new AgentPlayerAggregateModel
        {
            BrandId = brand.Id,
            AgentId = agentId,
            From = brand.CurrentSettlementFrom,
            To = brand.CurrentSettlementTo
        };
    }
}