using System.Runtime.CompilerServices;
using System.Transactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Tgs.Agent.Dividend.Domain.Enums;
using Tgs.Agent.Dividend.Domain.Interfaces.Repositories;
using Tgs.Agent.Dividend.Domain.Models;
using Tgs.Agent.Dividend.Repository.Entities;

namespace Tgs.Agent.Dividend.Repository;

internal class Repository(tgsContext context, IMemoryCache memoryCache) : IRepository
{
    private readonly HashSet<string> _cacheKeys = [];
    public async IAsyncEnumerable<AgentModel> GetAgentsCacheAsyncEnumerable(Guid brandId,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var key = $"AgentCache{brandId}";

        _cacheKeys.Add(key);

        var models = await memoryCache.GetOrCreateAsync(key,
            cacheEntry =>
            {
                cacheEntry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);

                return context.Agents.Where(x => x.BrandId == brandId)
                    .Select(x => new AgentModel
                    {
                        Id = x.Id,
                        BrandId = x.BrandId,
                        Username = x.Username,
                        ParentAgentId = x.ParentAgentId,
                        Path = x.Path,
                        DividendPercentage = x.DividendPercentage
                    })
                    .ToArrayAsync(cancellationToken);
            }) ?? [];

        foreach (var model in models) yield return model;
    }

    public IAsyncEnumerable<AgentModel> GetAgentsByPathCacheAsyncEnumerable(Guid brandId,
        LTree path,
        CancellationToken cancellationToken)
    {
        return GetAgentsCacheAsyncEnumerable(brandId, cancellationToken)
            .Where(x => path.IsDescendantOf(x.Path));
    }

    public IAsyncEnumerable<Guid> GetNonArchivedBrandIdsAsyncEnumerable(Guid? brandId)
    {
        return context.Brands
            .Where(x => brandId == null || x.Id == brandId)
            .Where(x => x.Status != (short)BrandStatus.Archived)
            .Where(x => DateTime.UtcNow.AddDays(-1) > x.CurrentSettlementTo)
            .Select(x => x.Id)
            .ToAsyncEnumerable();
    }

    public async IAsyncEnumerable<AgentPlayerAggregateModel> GetAgentPlayerAggregatesCacheAsync(Guid brandId,
        DateTime from,
        DateTime to,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var key = $"AgentPlayerAggregateCache{brandId}{from}{to}";

        _cacheKeys.Add(key);

        var models = await memoryCache.GetOrCreateAsync(key,
            async cacheEntry =>
            {
                cacheEntry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);

                var playerGameAggregate = await context.PlayerGameRebateAggregates
                    .Where(x => x.AgentId != null)
                    .Where(x => x.BrandId == brandId)
                    .Where(x => x.ReportDate >= from &&
                                x.ReportDate <= to)
                    .GroupBy(x => new
                        {
                            x.ReportDate,
                            x.Hour,
                            AgentId = x.AgentId!.Value,
                            x.BrandId
                        },
                        (agent, playerGames) => new
                        {
                            agent.ReportDate,
                            agent.Hour,
                            agent.AgentId,
                            agent.BrandId,
                            LiveDealerWinLoss =
                                playerGames
                                    .Where(l => l.GameType == (short)GameType.LiveDealer)
                                    .Sum(l => l.WinLoss),
                            SlotWinLoss =
                                playerGames
                                    .Where(l => l.GameType == (short)GameType.Slot)
                                    .Sum(l => l.WinLoss),
                            FishingWinLoss =
                                playerGames
                                    .Where(l => l.GameType == (short)GameType.Fishing)
                                    .Sum(l => l.WinLoss),
                            ArcadeWinLoss =
                                playerGames
                                    .Where(l => l.GameType == (short)GameType.Arcade)
                                    .Sum(l => l.WinLoss),
                            TableGameWinLoss =
                                playerGames
                                    .Where(l => l.GameType == (short)GameType.TableGame)
                                    .Sum(l => l.WinLoss),
                            LotteryWinLoss =
                                playerGames
                                    .Where(l => l.GameType == (short)GameType.Lottery)
                                    .Sum(l => l.WinLoss),
                            CardGameWinLoss =
                                playerGames
                                    .Where(l => l.GameType == (short)GameType.CardGame)
                                    .Sum(l => l.WinLoss),
                            SportsWinLoss =
                                playerGames
                                    .Where(l => l.GameType == (short)GameType.Sportsbook)
                                    .Sum(l => l.WinLoss),
                            ESportsWinLoss =
                                playerGames
                                    .Where(l => l.GameType == (short)GameType.ESports)
                                    .Sum(l => l.WinLoss),
                            VirtualSportsWinLoss =
                                playerGames
                                    .Where(l => l.GameType == (short)GameType.VirtualSports)
                                    .Sum(l => l.WinLoss),
                            WinLoss = playerGames
                                .Sum(l => l.WinLoss)
                        })
                    .ToArrayAsync(cancellationToken);

                var playerAggregate = await context.PlayerAggregates
                    .Where(x => x.BrandId == brandId)
                    .Where(x => x.AgentId != null)
                    .Where(x => x.ReportDate >= from && x.ReportDate <= to)
                    .GroupBy(x => new
                    {
                        x.ReportDate,
                        x.Hour,
                        x.BrandId,
                        x.AgentId,
                        x.AgentDividendPercentage,
                        x.AgentDividendPercentageTreeData
                    })
                    .Select(x => new
                    {
                        x.Key.ReportDate,
                        x.Key.Hour,
                        x.Key.BrandId,
                        AgentId = x.Key.AgentId!.Value,
                        AgentDividendPercentage = x.Key.AgentDividendPercentage ?? 0M,
                        x.Key.AgentDividendPercentageTreeData,
                        PromotionAmount = x.Sum(p => p.PromotionAmount),
                        RebateAmount = x.Sum(p => p.RebateAmount),
                        DepositAmount = x.Sum(p => p.DepositAmount),
                        WithdrawalAmount = x.Sum(p => p.WithdrawalAmount),
                        CompanyAdjustAmount = x.Sum(p => p.CompanyAdjustAmount),
                        AgentAdjustAmount = x.Sum(p => p.AgentAdjustAmount),
                        BonusAdjustAmount = x.Sum(p => p.BonusAdjustAmount),
                        BonusAmount = x.Sum(p =>
                            p.RebateAmount + p.PromotionAmount + p.UpgradeAmount + p.SeasonAmount + p.BirthdayAmount),
                        BetAmount = x.Sum(p => p.BetAmount),
                        ValidBetAmount = x.Sum(p => p.ValidBetAmount),
                        OriginalTurnover = x.Sum(p => p.OriginalTurnover),
                        Turnover = x.Sum(p => p.Turnover),
                        WinLossAmount = x.Sum(p => p.WinLossAmount),
                        NoIncludeNetProfitCompanyAdjustAmount =
                            x.Sum(p => p.NoIncludeNetProfitCompanyAdjustAmount),
                        NoIncludeNetProfitAgentAdjustAmount =
                            x.Sum(p => p.NoIncludeNetProfitAgentAdjustAmount),
                        NoIncludeNetProfitBonusAdjustAmount =
                            x.Sum(p => p.NoIncludeNetProfitBonusAdjustAmount)
                    })
                    .ToArrayAsync(cancellationToken);

                return playerAggregate
                    .GroupJoin(playerGameAggregate,
                        agg => new { agg.ReportDate, agg.Hour, agg.AgentId, agg.BrandId },
                        aggGame => new { aggGame.ReportDate, aggGame.Hour, aggGame.AgentId, aggGame.BrandId },
                        (agg, list) => new
                        {
                            agg,
                            list
                        })
                    .SelectMany(x => x.list.DefaultIfEmpty(),
                        (x, gameAgg) => new
                        {
                            x.agg,
                            gameAgg
                        })
                    .GroupBy(x => new
                        {
                            x.agg.ReportDate,
                            x.agg.Hour,
                            x.agg.BrandId,
                            x.agg.AgentId,
                            x.agg.AgentDividendPercentage,
                            x.agg.AgentDividendPercentageTreeData,
                            x.agg.PromotionAmount,
                            x.agg.RebateAmount,
                            x.agg.DepositAmount,
                            x.agg.WithdrawalAmount,
                            x.agg.CompanyAdjustAmount,
                            x.agg.AgentAdjustAmount,
                            x.agg.BonusAdjustAmount,
                            x.agg.BonusAmount,
                            x.agg.BetAmount,
                            x.agg.ValidBetAmount,
                            x.agg.OriginalTurnover,
                            x.agg.Turnover,
                            x.agg.WinLossAmount,
                            x.agg.NoIncludeNetProfitCompanyAdjustAmount,
                            x.agg.NoIncludeNetProfitAgentAdjustAmount,
                            x.agg.NoIncludeNetProfitBonusAdjustAmount
                        },
                        (groupItem, list) => new
                        {
                            groupItem.ReportDate,
                            groupItem.Hour,
                            groupItem.AgentId,
                            groupItem.BrandId,
                            groupItem.AgentDividendPercentage,
                            groupItem.AgentDividendPercentageTreeData,
                            groupItem.PromotionAmount,
                            groupItem.RebateAmount,
                            groupItem.DepositAmount,
                            groupItem.WithdrawalAmount,
                            groupItem.CompanyAdjustAmount,
                            groupItem.AgentAdjustAmount,
                            groupItem.BonusAdjustAmount,
                            groupItem.BonusAmount,
                            groupItem.BetAmount,
                            groupItem.ValidBetAmount,
                            groupItem.OriginalTurnover,
                            groupItem.Turnover,
                            groupItem.WinLossAmount,
                            groupItem.NoIncludeNetProfitCompanyAdjustAmount,
                            groupItem.NoIncludeNetProfitAgentAdjustAmount,
                            groupItem.NoIncludeNetProfitBonusAdjustAmount,
                            LiveDealerWinLoss = list.Sum(x => x.gameAgg?.LiveDealerWinLoss ?? 0M),
                            SlotWinLoss = list.Sum(x => x.gameAgg?.SlotWinLoss ?? 0M),
                            FishingWinLoss = list.Sum(x => x.gameAgg?.FishingWinLoss ?? 0M),
                            ArcadeWinLoss = list.Sum(x => x.gameAgg?.ArcadeWinLoss ?? 0M),
                            TableGameWinLoss = list.Sum(x => x.gameAgg?.TableGameWinLoss ?? 0M),
                            LotteryWinLoss = list.Sum(x => x.gameAgg?.LotteryWinLoss ?? 0M),
                            CardGameWinLoss = list.Sum(x => x.gameAgg?.CardGameWinLoss ?? 0M),
                            SportsWinLoss = list.Sum(x => x.gameAgg?.SportsWinLoss ?? 0M),
                            ESportsWinLoss = list.Sum(x => x.gameAgg?.ESportsWinLoss ?? 0M),
                            VirtualSportsWinLoss = list.Sum(x => x.gameAgg?.VirtualSportsWinLoss ?? 0M)
                        })
                    .GroupBy(x => new
                        {
                            x.BrandId,
                            x.AgentId,
                            x.AgentDividendPercentage,
                            x.AgentDividendPercentageTreeData
                        },
                        (groupItem, list) => new AgentPlayerAggregateModel
                        {
                            AgentId = groupItem.AgentId,
                            BrandId = groupItem.BrandId,
                            AgentDividendPercentage = groupItem.AgentDividendPercentage,
                            AgentDividendPercentageTreeData = groupItem.AgentDividendPercentageTreeData,
                            PromotionAmount = list.Sum(x => x.PromotionAmount),
                            RebateAmount = list.Sum(x => x.RebateAmount),
                            DepositAmount = list.Sum(x => x.DepositAmount),
                            WithdrawalAmount = list.Sum(x => x.WithdrawalAmount),
                            CompanyAdjustAmount = list.Sum(x => x.CompanyAdjustAmount),
                            AgentAdjustAmount = list.Sum(x => x.AgentAdjustAmount),
                            BonusAdjustAmount = list.Sum(x => x.BonusAdjustAmount),
                            BonusAmount = list.Sum(x => x.BonusAmount),
                            BetAmount = list.Sum(x => x.BetAmount),
                            ValidBetAmount = list.Sum(x => x.ValidBetAmount),
                            OriginalTurnover = list.Sum(x => x.OriginalTurnover),
                            Turnover = list.Sum(x => x.Turnover),
                            WinLossAmount = list.Sum(x => x.WinLossAmount),
                            NoIncludeNetProfitCompanyAdjustAmount =
                                list.Sum(x => x.NoIncludeNetProfitCompanyAdjustAmount),
                            NoIncludeNetProfitAgentAdjustAmount =
                                list.Sum(x => x.NoIncludeNetProfitAgentAdjustAmount),
                            NoIncludeNetProfitBonusAdjustAmount =
                                list.Sum(x => x.NoIncludeNetProfitBonusAdjustAmount),
                            LiveDealerWinLoss = list.Sum(x => x.LiveDealerWinLoss),
                            SlotWinLoss = list.Sum(x => x.SlotWinLoss),
                            FishingWinLoss = list.Sum(x => x.FishingWinLoss),
                            ArcadeWinLoss = list.Sum(x => x.ArcadeWinLoss),
                            TableGameWinLoss = list.Sum(x => x.TableGameWinLoss),
                            LotteryWinLoss = list.Sum(x => x.LotteryWinLoss),
                            CardGameWinLoss = list.Sum(x => x.CardGameWinLoss),
                            SportsWinLoss = list.Sum(x => x.SportsWinLoss),
                            ESportsWinLoss = list.Sum(x => x.ESportsWinLoss),
                            VirtualSportsWinLoss = list.Sum(x => x.VirtualSportsWinLoss)
                        })
                    .ToArray();
            }) ?? [];

        foreach (var model in models) yield return model;
    }

    public async IAsyncEnumerable<AgentPlayerAggregateModel> GetAgentPlayerAggregatesForMultipleLayerCacheAsync(
        Guid brandId,
        DateTime from,
        DateTime to,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var key = $"AgentPlayerAggregatesForMultipleLayerCache{brandId}{from}{to}";

        _cacheKeys.Add(key);

        var models = await memoryCache.GetOrCreateAsync(key,
            cacheEntry =>
            {
                cacheEntry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);

                return context.PlayerAggregates
                    .Where(x => x.BrandId == brandId)
                    .Where(x => x.AgentId != null)
                    .Where(x => x.ReportDate >= from && x.ReportDate <= to)
                    .GroupBy(x => new
                    {
                        x.BrandId,
                        x.AgentId
                    })
                    .Select(x => new AgentPlayerAggregateModel
                    {
                        AgentId = x.Key.AgentId!.Value,
                        BrandId = x.Key.BrandId,
                        From = from,
                        To = to,
                        PromotionAmount = x.Sum(p => p.PromotionAmount),
                        RebateAmount = x.Sum(p => p.RebateAmount),
                        DepositAmount = x.Sum(p => p.DepositAmount),
                        WithdrawalAmount = x.Sum(p => p.WithdrawalAmount),
                        CompanyAdjustAmount = x.Sum(p => p.CompanyAdjustAmount),
                        AgentAdjustAmount = x.Sum(p => p.AgentAdjustAmount),
                        BonusAdjustAmount = x.Sum(p => p.BonusAdjustAmount),
                        BonusAmount = x.Sum(p =>
                            p.RebateAmount + p.PromotionAmount + p.UpgradeAmount + p.SeasonAmount + p.BirthdayAmount),
                        BetAmount = x.Sum(p => p.BetAmount),
                        ValidBetAmount = x.Sum(p => p.ValidBetAmount),
                        OriginalTurnover = x.Sum(p => p.OriginalTurnover),
                        Turnover = x.Sum(p => p.Turnover),
                        WinLossAmount = x.Sum(p => p.WinLossAmount),
                        NoIncludeNetProfitCompanyAdjustAmount =
                            x.Sum(p => p.NoIncludeNetProfitCompanyAdjustAmount),
                        NoIncludeNetProfitAgentAdjustAmount =
                            x.Sum(p => p.NoIncludeNetProfitAgentAdjustAmount),
                        NoIncludeNetProfitBonusAdjustAmount =
                            x.Sum(p => p.NoIncludeNetProfitBonusAdjustAmount)
                    })
                    .ToArrayAsync(cancellationToken);
            }) ?? [];

        foreach (var model in models) yield return model;
    }

    public IAsyncEnumerable<AgentPlayerGameAggregateModel> GetAgentPlayerGameAggregatesCacheAsync(Guid brandId,
        DateTime from,
        DateTime to)
    {
        return context.PlayerGameRebateAggregates
            .Where(x => x.AgentId != null)
            .Where(x => x.BrandId == brandId)
            .Where(x => x.ReportDate >= from && x.ReportDate <= to)
            .GroupBy(x => x.AgentId,
                (agentId, playerGames) => new AgentPlayerGameAggregateModel
                {
                    AgentId = agentId!.Value,
                    LiveDealerWinLoss =
                        playerGames
                            .Where(l => l.GameType == (short)GameType.LiveDealer)
                            .Sum(l => l.WinLoss),
                    SlotWinLoss =
                        playerGames
                            .Where(l => l.GameType == (short)GameType.Slot)
                            .Sum(l => l.WinLoss),
                    FishingWinLoss =
                        playerGames
                            .Where(l => l.GameType == (short)GameType.Fishing)
                            .Sum(l => l.WinLoss),
                    ArcadeWinLoss =
                        playerGames
                            .Where(l => l.GameType == (short)GameType.Arcade)
                            .Sum(l => l.WinLoss),
                    TableGameWinLoss =
                        playerGames
                            .Where(l => l.GameType == (short)GameType.TableGame)
                            .Sum(l => l.WinLoss),
                    LotteryWinLoss =
                        playerGames
                            .Where(l => l.GameType == (short)GameType.Lottery)
                            .Sum(l => l.WinLoss),
                    CardGameWinLoss =
                        playerGames
                            .Where(l => l.GameType == (short)GameType.CardGame)
                            .Sum(l => l.WinLoss),
                    SportsWinLoss =
                        playerGames
                            .Where(l => l.GameType == (short)GameType.Sportsbook)
                            .Sum(l => l.WinLoss),
                    ESportsWinLoss =
                        playerGames
                            .Where(l => l.GameType == (short)GameType.ESports)
                            .Sum(l => l.WinLoss),
                    VirtualSportsWinLoss =
                        playerGames
                            .Where(l => l.GameType == (short)GameType.VirtualSports)
                            .Sum(l => l.WinLoss)
                })
            .ToAsyncEnumerable();
    }

    public async IAsyncEnumerable<AgentPlayerDepositModel> GetAgentPlayerDepositsCacheAsync(Guid brandId,
        DateOnly from,
        DateOnly to,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var key = $"AgentPlayerDepositCache{brandId}{from}{to}";

        _cacheKeys.Add(key);

        var models = await memoryCache.GetOrCreateAsync(key,
            cacheEntry =>
            {
                cacheEntry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);

                return context.PlayerDailyPaymentStatistics
                    .Where(x => x.ReportDate >= from && x.ReportDate < to.AddDays(1))
                    .Join(context.Players,
                        pp => pp.PlayerCode,
                        p => p.PlayerCode,
                        (pp, p) => new { pp.PlayerCode, pp.DailyDepositAmount, p.AgentId, p.BrandId })
                    .Where(x => x.BrandId == brandId && x.AgentId != null)
                    .GroupBy(x => new { x.BrandId, x.AgentId, x.PlayerCode })
                    .Select(x => new AgentPlayerDepositModel
                    {
                        BrandId = x.Key.BrandId,
                        AgentId = x.Key.AgentId!.Value,
                        PlayerCode = x.Key.PlayerCode,
                        From = from,
                        To = to,
                        DepositAmount = x.Sum(s => s.DailyDepositAmount ?? 0M)
                    })
                    .ToArrayAsync(cancellationToken);
            }) ?? [];

        foreach (var model in models) yield return model;
    }

    public async IAsyncEnumerable<AgentPlayerValidBetModel> GetAgentPlayerValidBetsCacheAsync(Guid brandId,
        DateTime from,
        DateTime to,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var key = $"AgentPlayerValidBetCache{brandId}{from}{to}";

        _cacheKeys.Add(key);

        var models = await memoryCache.GetOrCreateAsync(key,
            cacheEntry =>
            {
                cacheEntry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);

                return context.PlayerAggregates.Where(x => x.ReportDate >= from && x.ReportDate < to.AddDays(1))
                    .Where(x => x.BrandId == brandId && x.AgentId != null)
                    .GroupBy(x => new { x.BrandId, x.AgentId, x.PlayerCode })
                    .Select(x => new AgentPlayerValidBetModel
                    {
                        BrandId = x.Key.BrandId,
                        AgentId = x.Key.AgentId!.Value,
                        PlayerCode = x.Key.PlayerCode,
                        From = from,
                        To = to,
                        ValidBetAmount = x.Sum(s => s.Turnover)
                    })
                    .ToArrayAsync(cancellationToken);
            }) ?? [];

        foreach (var model in models) yield return model;
    }

    public async IAsyncEnumerable<AgentPlayerBetDaysModel> GetAgentPlayerBetCountsCacheAsync(Guid brandId,
        DateTime from,
        DateTime to,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var key = $"AgentPlayerBetCountCache{brandId}{from}{to}";

        _cacheKeys.Add(key);

        var models = await memoryCache.GetOrCreateAsync(key,
            cacheEntry =>
            {
                cacheEntry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);

                return context.PlayerAggregates.Where(x => x.ReportDate >= from && x.ReportDate < to.AddDays(1))
                    .Where(x => x.BrandId == brandId && x.AgentId != null)
                    .Where(x => x.BetAmount > 0)
                    .GroupBy(x => new { x.ReportDate, x.BrandId, x.AgentId, x.PlayerCode })
                    .Select(x => new AgentPlayerBetDaysModel
                    {
                        BrandId = x.Key.BrandId,
                        AgentId = x.Key.AgentId!.Value,
                        PlayerCode = x.Key.PlayerCode,
                        From = from,
                        To = to,
                        DayCount = x.Select(s => s.ReportDate).Distinct().Count()
                    })
                    .ToArrayAsync(cancellationToken);
            }) ?? [];

        foreach (var model in models) yield return model;
    }

    public IAsyncEnumerable<NegativeAgentDividendModel> GetNegativeAgentDividendsAsync(Guid agent)
    {
        return context.AgentDividends.Where(x => x.AgentId == agent && x.SentOn == null)
            .Select(x => new NegativeAgentDividendModel(x.Id, x.AgentId, x.AgentDividendAmount))
            .ToAsyncEnumerable();
    }

    public async Task CreateAgentDividendsAsync(BrandModel brand,
        IEnumerable<CreateAgentDividendModel> agentDividends,
        IDictionary<int, int> sendNegativeDividendIds,
        CancellationToken cancellationToken)
    {
        using var scope = new TransactionScope(TransactionScopeOption.Required,
            new TransactionOptions
                { Timeout = TimeSpan.FromMinutes(10) },
            TransactionScopeAsyncFlowOption.Enabled);

        var negativeDividends = await context.AgentDividends.Where(x => sendNegativeDividendIds.Keys.Contains(x.Id))
            .ToArrayAsync(cancellationToken);

        foreach (var negativeDividend in negativeDividends)
            negativeDividend.ReferencedAgentDividendId = sendNegativeDividendIds[negativeDividend.Id];
        //negativeDividend.SentOn = DateTime.UtcNow;
        await CreateAgentDividendsAsync(agentDividends);

        context.AgentDividends.UpdateRange(negativeDividends);

        await context.Brands.Where(x => x.Id == brand.Id)
            .ExecuteUpdateAsync(s =>
                    s.SetProperty(e => e.PreviousSettlementFrom, brand.PreviousSettlementFrom)
                        .SetProperty(e => e.PreviousSettlementTo, brand.PreviousSettlementTo)
                        .SetProperty(e => e.CurrentSettlementFrom, brand.CurrentSettlementFrom)
                        .SetProperty(e => e.CurrentSettlementTo, brand.CurrentSettlementTo)
                        .SetProperty(e => e.NextSettlementFrom, brand.NextSettlementFrom)
                        .SetProperty(e => e.NextSettlementTo, brand.NextSettlementTo),
                cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        scope.Complete();
    }

    public Task<BrandModel> GetBrandAsync(Guid brandId, CancellationToken cancellationToken)
    {
        return context.Brands.Where(x => x.Id == brandId)
            .Select(x => new BrandModel
            {
                Id = x.Id,
                SettlementIntervalType = x.SettlementIntervalType,
                PreviousSettlementFrom = x.PreviousSettlementFrom,
                PreviousSettlementTo = x.PreviousSettlementTo,
                CurrentSettlementFrom = x.CurrentSettlementFrom,
                CurrentSettlementTo = x.CurrentSettlementTo,
                NextSettlementFrom = x.NextSettlementFrom,
                NextSettlementTo = x.NextSettlementTo,
                AutoSentAgentBonus = x.AutoSentAgentBonus,
                Code = x.Code,
                SettlementDay = x.SettlementDay,
                PlatformCharge = x.PlatformCharge,
                AgentDividendType = x.AgentDividendType
            })
            .FirstAsync(cancellationToken);
    }

    public Task<int> GetMaxAgentDividendIdAsync(CancellationToken cancellationToken)
    {
        return context.AgentDividends.MaxAsync(x => x.Id, cancellationToken);
    }

    public async Task UpdateSentOnAsync(int agentDividendId, CancellationToken cancellationToken)
    {
        await context.AgentDividends.Where(x => x.ReferencedAgentDividendId == agentDividendId)
            .ExecuteUpdateAsync(s =>
                    s.SetProperty(e => e.SentOn, DateTime.UtcNow),
                cancellationToken);

        await context.AgentDividends.Where(x => x.Id == agentDividendId)
            .ExecuteUpdateAsync(s =>
                    s.SetProperty(e => e.SentOn, DateTime.UtcNow),
                cancellationToken);

        await context.AgentDividendRecords.Where(x => x.AgentDividendId == agentDividendId)
            .ExecuteUpdateAsync(s =>
                    s.SetProperty(e => e.SentOn, DateTime.UtcNow),
                cancellationToken);

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task CreateAgentDividendRecordAsync(Guid brandId,
        Guid agentId,
        int agentDividendId,
        decimal agentDividendAmount,
        CancellationToken cancellationToken)
    {
        var negativeDividends = await context.AgentDividends.Where(x => x.ReferencedAgentDividendId == agentDividendId)
            .ToArrayAsync(cancellationToken);

        _ = await context.AgentDividendRecords.AddAsync(new AgentDividendRecord
            {
                BrandId = brandId,
                AgentId = agentId,
                AgentDividendId = agentDividendId,
                NegativeDividendIds = negativeDividends.Select(x => x.Id.ToString()).ToList(),
                AgentDividendAmount = agentDividendAmount,
                CreatedOn = DateTime.UtcNow
            },
            cancellationToken);

        await context.SaveChangesAsync(cancellationToken);
    }

    public void RemoveAllMemoryCache(CancellationToken cancellationToken)
    {
        foreach (var cacheKey in _cacheKeys) memoryCache.Remove(cacheKey);
    }

    private async ValueTask CreateAgentDividendsAsync(IEnumerable<CreateAgentDividendModel> models)
    {
        await context.AgentDividends.AddRangeAsync(models.Select(model => new AgentDividend
            {
                Id = model.Id,
                BrandId = model.BrandId,
                AgentId = model.AgentId,
                From = model.From.UtcDateTime,
                To = model.To.UtcDateTime,
                AgentDividendPercentage = model.AgentDividendPercentage,
                PromotionAmount = model.PromotionAmount,
                RebateAmount = model.RebateAmount,
                DepositAmount = model.DepositAmount,
                WithdrawalAmount = model.WithdrawalAmount,
                CompanyAdjustAmount = model.CompanyAdjustAmount,
                AgentAdjustAmount = model.AgentAdjustAmount,
                BonusAdjustAmount = model.BonusAdjustAmount,
                BonusAmount = model.BonusAmount,
                BetAmount = model.BetAmount,
                ValidBetAmount = model.ValidBetAmount,
                OriginalTurnover = model.OriginalTurnover,
                Turnover = model.Turnover,
                WinLossAmount = model.WinLossAmount,
                NoIncludeNetProfitCompanyAdjustAmount = model.NoIncludeNetProfitCompanyAdjustAmount,
                NoIncludeNetProfitAgentAdjustAmount = model.NoIncludeNetProfitAgentAdjustAmount,
                NoIncludeNetProfitBonusAdjustAmount = model.NoIncludeNetProfitBonusAdjustAmount,
                NetProfit = model.NetProfit,
                LiveDealerWinLoss = model.LiveDealerWinLoss,
                SlotWinLoss = model.SlotWinLoss,
                FishingWinLoss = model.FishingWinLoss,
                ArcadeWinLoss = model.ArcadeWinLoss,
                TableGameWinLoss = model.TableGameWinLoss,
                LotteryWinLoss = model.LotteryWinLoss,
                CardGameWinLoss = model.CardGameWinLoss,
                SportsWinLoss = model.SportsWinLoss,
                ESportsWinLoss = model.ESportsWinLoss,
                VirtualSportsWinLoss = model.VirtualSportsWinLoss,
                AgentDividendAmount = model.AgentDividendAmount,
                CreatedOn = DateTime.UtcNow,
                SentOn = null,
                BeforeDividendAmount = model.BeforeDividendAmount,
                ReferencedAgentDividendId = model.ReferencedAgentDividendId,
                AgentDividendMultipleLayers = model.AgentDividendMultipleLayerModels.Select(x
                        => new AgentDividendMultipleLayer
                        {
                            AgentId = x.AgentId,
                            AgentDividendId = model.Id,
                            AgentDividendPercentage = x.AgentDividendPercentage,
                            ActualDividendPercentage = x.ActualDividendPercentage,
                            AgentCoveredBonus = x.AgentCoveredBonus,
                            AgentCoveredBonusAdjust = x.AgentCoveredBonusAdjust,
                            DividendProviderAgentId = x.DividendProviderAgentId,
                            DividendProviderId = x.DividendProviderId,
                            Layer = x.Layer,
                            Dividend = x.Dividend
                        })
                    .ToArray()
            })
        );
    }
}