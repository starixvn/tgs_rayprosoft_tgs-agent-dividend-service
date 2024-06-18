using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Transactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Tgs.Agent.Dividend.Domain;
using Tgs.Agent.Dividend.Domain.Enums;
using Tgs.Agent.Dividend.Domain.Interfaces.Repositories;
using Tgs.Agent.Dividend.Domain.Models;
using Tgs.Agent.Dividend.Repository.Entities;
using TGS.Core.Infrastructures.Extensions;

namespace Tgs.Agent.Dividend.Repository;

internal class Repository(tgsContext context, IMemoryCache memoryCache, IMemoryCacheKeyMapper memoryCacheKeyMapper)
    : IRepository
{
    public async IAsyncEnumerable<AgentModel> GetAgentsCacheAsyncEnumerable(Guid brandId,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var key = $"AgentCache{brandId}";

        memoryCacheKeyMapper.AddCacheKey(key);

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
                                     DividendPercentage = x.DividendPercentage,
                                     NegativeAgentDividendCarryForward = x.NegativeAgentDividendCarryForward
                                 })
                                 .ToArrayAsync(cancellationToken);
                         }) ??
                     [];

        foreach (var model in models) yield return model;
    }

    public IAsyncEnumerable<AgentModel> GetDescendantAgentsByPathCacheAsyncEnumerable(Guid brandId,
        LTree path,
        CancellationToken cancellationToken)
    {
        return context.Agents.Where(x => x.BrandId == brandId)
            .Where(x => path.IsDescendantOf(x.Path))
            .Select(x => new AgentModel
            {
                Id = x.Id,
                BrandId = x.BrandId,
                Username = x.Username,
                ParentAgentId = x.ParentAgentId,
                Path = x.Path,
                DividendPercentage = x.DividendPercentage
            })
            .ToAsyncEnumerable();
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

        memoryCacheKeyMapper.AddCacheKey(key);

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
                                         p.RebateAmount +
                                         p.PromotionAmount +
                                         p.UpgradeAmount +
                                         p.SeasonAmount +
                                         p.BirthdayAmount),
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
                                 .Select(x =>
                                 {
                                     var treeData = JsonSerializer.Deserialize<AgentDividendPercentageData[]>(x
                                         .AgentDividendPercentageTreeData!)!;

                                     var treeDataString = JsonSerializer.Serialize(treeData.OrderBy(t => t.AgentId));

                                     return new
                                     {
                                         x.ReportDate,
                                         x.Hour,
                                         x.BrandId,
                                         x.AgentId,
                                         x.AgentDividendPercentage,
                                         AgentDividendPercentageTreeData = treeDataString,
                                         x.PromotionAmount,
                                         x.RebateAmount,
                                         x.DepositAmount,
                                         x.WithdrawalAmount,
                                         x.CompanyAdjustAmount,
                                         x.AgentAdjustAmount,
                                         x.BonusAdjustAmount,
                                         x.BonusAmount,
                                         x.BetAmount,
                                         x.ValidBetAmount,
                                         x.OriginalTurnover,
                                         x.Turnover,
                                         x.WinLossAmount,
                                         x.NoIncludeNetProfitCompanyAdjustAmount,
                                         x.NoIncludeNetProfitAgentAdjustAmount,
                                         x.NoIncludeNetProfitBonusAdjustAmount
                                     };
                                 })
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
                                     x.Key.AgentId,
                                     x.Key.AgentDividendPercentage,
                                     x.Key.AgentDividendPercentageTreeData,
                                     PromotionAmount = x.Sum(p => p.PromotionAmount),
                                     RebateAmount = x.Sum(p => p.RebateAmount),
                                     DepositAmount = x.Sum(p => p.DepositAmount),
                                     WithdrawalAmount = x.Sum(p => p.WithdrawalAmount),
                                     CompanyAdjustAmount = x.Sum(p => p.CompanyAdjustAmount),
                                     AgentAdjustAmount = x.Sum(p => p.AgentAdjustAmount),
                                     BonusAdjustAmount = x.Sum(p => p.BonusAdjustAmount),
                                     BonusAmount = x.Sum(p => p.BonusAmount),
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
                                 .GroupJoin(playerGameAggregate,
                                     agg => new { agg.ReportDate, agg.Hour, agg.AgentId, agg.BrandId },
                                     aggGame => new
                                         { aggGame.ReportDate, aggGame.Hour, aggGame.AgentId, aggGame.BrandId },
                                     (agg, list) => new
                                     {
                                         agg,
                                         list
                                     })
                                 .SelectMany(x => x.list.DefaultIfEmpty(),
                                     (x, gameAgg) =>
                                         new
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
                                         From = from,
                                         To = to,
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
                         }) ??
                     [];

        foreach (var model in models) yield return model;
    }

    public async IAsyncEnumerable<AgentDividendProductFeeModel> GetAgentProductFeeCacheAsync(Guid brandId,
        DateTime from,
        DateTime to,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var key = $"AgentProductFeeCache{brandId}{from}{to}";

        memoryCacheKeyMapper.AddCacheKey(key);

        var models = await memoryCache.GetOrCreateAsync(key,
                         cacheEntry =>
                         {
                             cacheEntry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);

                             return context.PlayerGameRebateAggregates
                                 .Where(x => x.BrandId == brandId)
                                 .Where(x => x.AgentId != null)
                                 .Where(x => x.ReportDate >= from && x.ReportDate <= to)
                                 .GroupBy(x => x.AgentId)
                                 .Select(x => new
                                 {
                                     AgentId = x.Key,
                                     CardGameWinLossAmount = x.Where(w => w.GameType == (short)GameType.CardGame).Sum(p => p.WinLoss),
                                     LiveDealerWinLossAmount = x.Where(w => w.GameType == (short)GameType.LiveDealer).Sum(p => p.WinLoss),
                                     LotteryWinLossAmount = x.Where(w => w.GameType == (short)GameType.Lottery).Sum(p => p.WinLoss),
                                     SlotWinLossAmount = x.Where(w => Consts.SlotGroup.Select(s => (short)s).Contains(w.GameType)).Sum(p => p.WinLoss),
                                     SportsWinLossAmount = x.Where(w => Consts.SportsGroup.Select(s => (short)s).Contains(w.GameType)).Sum(p => p.WinLoss),
                                     VirtualSportsWinLossAmount = x.Where(w => w.GameType == (short)GameType.VirtualSports).Sum(p => p.WinLoss),
                                 })
                                 .GroupJoin(context.AgentProductFees,
                                     a => a.AgentId,
                                     a => a.AgentId,
                                     (aggregate, productFee) => new
                                     {
                                         aggregate,
                                         productFee = productFee.DefaultIfEmpty()
                                     })
                                 .SelectMany(x => x.productFee.DefaultIfEmpty(),
                                     (x, productFee) => new
                                     {
                                         x.aggregate,
                                         productFee
                                     })
                                 .Select(x => new AgentDividendProductFeeModel
                                 {
                                     AgentId = x.aggregate.AgentId!.Value,
                                     CurrentCardGamePercentage = x.productFee == null ? 0M : x.productFee.CurrentCardGamePercentage,
                                     CurrentLiveDealerPercentage = x.productFee == null ? 0M : x.productFee.CurrentLiveDealerPercentage,
                                     CurrentLotteryPercentage = x.productFee == null ? 0M : x.productFee.CurrentLotteryPercentage,
                                     CurrentSlotPercentage = x.productFee == null ? 0M : x.productFee.CurrentSlotPercentage,
                                     CurrentSportsPercentage = x.productFee == null ? 0M : x.productFee.CurrentSportsPercentage,
                                     CurrentVirtualSportsPercentage = x.productFee == null ? 0M : x.productFee.CurrentVirtualSportsPercentage,
                                     CardGameWinLossAmount = x.aggregate.CardGameWinLossAmount,
                                     LiveDealerWinLossAmount = x.aggregate.LiveDealerWinLossAmount,
                                     LotteryWinLossAmount = x.aggregate.LotteryWinLossAmount,
                                     SlotWinLossAmount = x.aggregate.SlotWinLossAmount,
                                     SportsWinLossAmount = x.aggregate.SportsWinLossAmount,
                                     VirtualSportsWinLossAmount = x.aggregate.VirtualSportsWinLossAmount,
                                 })
                                 .ToArrayAsync(cancellationToken);
                         }) ??
                     [];

        foreach (var model in models) yield return model;
    }

    public async IAsyncEnumerable<AgentPlayerAggregateModel> GetAgentPlayerAggregatesForMultipleLayerCacheAsync(
        Guid brandId,
        DateTime from,
        DateTime to,
        [EnumeratorCancellation] CancellationToken cancellationToken,
        bool includeCheckPlayerAgentRelation = false)
    {
        var key = $"AgentPlayerAggregatesForMultipleLayerCache{brandId}{from}{to}{includeCheckPlayerAgentRelation}";

        memoryCacheKeyMapper.AddCacheKey(key);

        var models = await memoryCache.GetOrCreateAsync(key,
                         cacheEntry =>
                         {
                             cacheEntry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);

                             return context.PlayerAggregates
                                 .Where(x => x.BrandId == brandId)
                                 .Where(x => x.AgentId != null)
                                 .Where(x => x.ReportDate >= from && x.ReportDate <= to)
                                 .GroupJoin(context.PlayerAgentRelations,
                                     pa => pa.PlayerCode,
                                     par => par.PlayerCode,
                                     (pa, par) => new
                                     {
                                         pa,
                                         par = par.DefaultIfEmpty()
                                     })
                                 .SelectMany(x => x.par.DefaultIfEmpty(),
                                     (x, playerAgentRelation) => new
                                     {
                                         x.pa,
                                         playerAgentRelation
                                     })
                                 .Where(includeCheckPlayerAgentRelation, x => x.playerAgentRelation == null)
                                 .GroupBy(x => new
                                 {
                                     x.pa.BrandId,
                                     x.pa.AgentId
                                 })
                                 .Select(x => new AgentPlayerAggregateModel
                                 {
                                     AgentId = x.Key.AgentId!.Value,
                                     BrandId = x.Key.BrandId,
                                     From = from,
                                     To = to,
                                     PromotionAmount = x.Sum(p => p.pa.PromotionAmount),
                                     RebateAmount = x.Sum(p => p.pa.RebateAmount),
                                     DepositAmount = x.Sum(p => p.pa.DepositAmount),
                                     WithdrawalAmount = x.Sum(p => p.pa.WithdrawalAmount),
                                     CompanyAdjustAmount = x.Sum(p => p.pa.CompanyAdjustAmount),
                                     AgentAdjustAmount = x.Sum(p => p.pa.AgentAdjustAmount),
                                     BonusAdjustAmount = x.Sum(p => p.pa.BonusAdjustAmount),
                                     BonusAmount = x.Sum(p =>
                                         p.pa.RebateAmount +
                                         p.pa.PromotionAmount +
                                         p.pa.UpgradeAmount +
                                         p.pa.SeasonAmount +
                                         p.pa.BirthdayAmount),
                                     BetAmount = x.Sum(p => p.pa.BetAmount),
                                     ValidBetAmount = x.Sum(p => p.pa.ValidBetAmount),
                                     OriginalTurnover = x.Sum(p => p.pa.OriginalTurnover),
                                     Turnover = x.Sum(p => p.pa.Turnover),
                                     WinLossAmount = x.Sum(p => p.pa.WinLossAmount),
                                     NoIncludeNetProfitCompanyAdjustAmount =
                                         x.Sum(p => p.pa.NoIncludeNetProfitCompanyAdjustAmount),
                                     NoIncludeNetProfitAgentAdjustAmount =
                                         x.Sum(p => p.pa.NoIncludeNetProfitAgentAdjustAmount),
                                     NoIncludeNetProfitBonusAdjustAmount =
                                         x.Sum(p => p.pa.NoIncludeNetProfitBonusAdjustAmount)
                                 })
                                 .ToArrayAsync(cancellationToken);
                         }) ??
                     [];

        foreach (var model in models) yield return model;
    }

    public async IAsyncEnumerable<AgentPlayerGameAggregateModel> GetAgentPlayerGameAggregatesCacheAsync(Guid brandId,
        DateTime from,
        DateTime to,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var key = $"AgentPlayerGameAggregatesCache{brandId}{from}{to}";

        memoryCacheKeyMapper.AddCacheKey(key);

        var models = await memoryCache.GetOrCreateAsync(key,
                         cacheEntry =>
                         {
                             cacheEntry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);

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
                                 .ToArrayAsync(cancellationToken);
                         }) ??
                     [];

        foreach (var model in models) yield return model;
    }

    public async IAsyncEnumerable<AgentPlayerDepositModel> GetAgentPlayerDepositsCacheAsync(Guid brandId,
        DateOnly from,
        DateOnly to,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var key = $"AgentPlayerDepositCache{brandId}{from}{to}";

        memoryCacheKeyMapper.AddCacheKey(key);

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
                         }) ??
                     [];

        foreach (var model in models) yield return model;
    }

    public async IAsyncEnumerable<AgentPlayerValidBetModel> GetAgentPlayerValidBetsCacheAsync(Guid brandId,
        DateTime from,
        DateTime to,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var key = $"AgentPlayerValidBetCache{brandId}{from}{to}";

        memoryCacheKeyMapper.AddCacheKey(key);

        var models = await memoryCache.GetOrCreateAsync(key,
                         cacheEntry =>
                         {
                             cacheEntry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);

                             return context.PlayerAggregates
                                 .Where(x => x.ReportDate >= from && x.ReportDate < to.AddDays(1))
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
                         }) ??
                     [];

        foreach (var model in models) yield return model;
    }

    public async IAsyncEnumerable<AgentPlayerBetDaysModel> GetAgentPlayerBetCountsCacheAsync(Guid brandId,
        DateTime from,
        DateTime to,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var key = $"AgentPlayerBetCountCache{brandId}{from}{to}";

        memoryCacheKeyMapper.AddCacheKey(key);

        var models = await memoryCache.GetOrCreateAsync(key,
                         cacheEntry =>
                         {
                             cacheEntry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);

                             return context.PlayerAggregates
                                 .Where(x => x.ReportDate >= from && x.ReportDate < to.AddDays(1))
                                 .Where(x => x.BrandId == brandId && x.AgentId != null)
                                 .Where(x => x.Turnover > 0)
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
                         }) ??
                     [];

        foreach (var model in models) yield return model;
    }

    public IAsyncEnumerable<NegativeAgentDividendModel> GetNegativeAgentDividendsAsync(Guid agentId)
    {
        return context.AgentDividends.Where(x => x.AgentId == agentId && x.SentOn == null)
            .Where(x => x.BeforeDividendAmount == null && x.ReferencedAgentDividendId == null)
            .Select(x => new NegativeAgentDividendModel(x.Id, x.AgentId, x.AgentDividendAmount))
            .ToAsyncEnumerable();
    }

    public async IAsyncEnumerable<AgentDividendBeforehand> GetAgentDividendBeforehandAsync(Guid agentId,
        DateTime from,
        DateTime to)
    {
        var agentDividends = await context.AgentDividends
            .Include(x => x.AgentDividendMultipleLayers)
            .Where(x => x.AgentId == agentId && x.From == from && x.To == to).ToArrayAsync();

        var agentDividendIds = agentDividends.Select(x => x.Id);

        var fees = await context.AgentDividendProductFees
            .Where(x => agentDividendIds.Contains(x.AgentDividendId)).ToArrayAsync();

        var records =
            await context.AgentDividendRecords.Where(x => agentDividendIds.Contains(x.AgentDividendId)).ToArrayAsync();

        foreach (var agentDividend in agentDividends)
        {
            var fee = fees.FirstOrDefault(f => f.AgentDividendId == agentDividend.Id) ?? new AgentDividendProductFee();

            var record = records.FirstOrDefault(f => f.AgentDividendId == agentDividend.Id);

            var cardGameAmount =
                fee.CardGameWinLossAmount <= 0M ? 0M : fee.CardGameWinLossAmount * fee.CardGamePercentage / 100;

            var liveDealerAmount =
                fee.LiveDealerWinLossAmount <= 0M ? 0M : fee.LiveDealerWinLossAmount * fee.LiveDealerPercentage / 100;

            var lotteryAmount =
                fee.LotteryWinLossAmount <= 0M ? 0M : fee.LotteryWinLossAmount * fee.LotteryPercentage / 100;

            var sportsAmount =
                fee.SportsWinLossAmount <= 0M ? 0M : fee.SportsWinLossAmount * fee.SportsPercentage / 100;

            var slotAmount =
                fee.SlotWinLossAmount <= 0M ? 0M : fee.SlotWinLossAmount * fee.SlotPercentage / 100;

            var virtualSportsAmount =
                fee.VirtualSportsWinLossAmount <= 0M
                    ? 0M
                    : fee.VirtualSportsWinLossAmount * fee.VirtualSportsPercentage / 100;

            var totalDividendAmount = agentDividend is null or
                { NegativeAgentDividendCarryForward: false, AgentDividendAmount: < 0M }
                ? record?.NegativeAgentDividendAmount ?? 0M
                : record?.ActualAgentDividendAmount ?? 0M;

            yield return new AgentDividendBeforehand(
                agentDividend.Id,
                agentDividend.From,
                agentDividend.To,
                agentDividend.ActiveNumber,
                agentDividend.WinLossAmount,
                agentDividend.BonusAmount * agentDividend.AgentDividendPercentage / 100,
                agentDividend.RebateAmount,
                agentDividend.NetProfit * agentDividend.AgentDividendPercentage / 100,
                agentDividend.AgentDividendPercentage,
                agentDividend.AgentDividendMultipleLayers.Where(w => w.DividendProviderAgentId != agentDividend.AgentId)
                    .Sum(s => s.Dividend),
                totalDividendAmount,
                agentDividend.BonusAdjustAmount * agentDividend.AgentDividendPercentage / 100,
                agentDividend.AgentAdjustAmount,
                cardGameAmount + liveDealerAmount + lotteryAmount + slotAmount + sportsAmount +
                virtualSportsAmount,
                record?.NegativeAgentDividendAmount ?? 0M,
                agentDividend.NegativeAgentDividendCarryForward,
                (DistributeStatus?)record?.Status,
                agentDividend.SentOn);
        }
    }

    public IAsyncEnumerable<AgentDividendRecordModel> GetAgentDividendRecordsAsync(Guid brandId,
        DistributeStatus? status,
        DateTime? from,
        DateTime? to,
        DateTime? approvedFrom,
        DateTime? approvedTo,
        string? agentUsername,
        bool? negativeAgentDividendCarryForward)
    {
        return context.AgentDividends.Where(x => x.BrandId == brandId)
            .Where(from != null, x => x.From >= from)
            .Where(to != null, x => x.To < to)
            .Join(context.AgentDividendRecords,
                ad => ad.Id,
                adr => adr.AgentDividendId,
                (ad, adr) => new
                {
                    adr.Id,
                    adr.BrandId,
                    adr.AgentId,
                    adr.Status,
                    ad.From,
                    ad.To,
                    adr.AgentDividendId,
                    adr.AgentDividendAmount,
                    adr.ActualAgentDividendAmount,
                    adr.NegativeAgentDividendAmount,
                    adr.NegativeAgentDividendCarryForward,
                    adr.Remark,
                    adr.CreatedOn,
                    adr.UpdatedOn,
                    adr.ApprovedOn
                })
            .Where(approvedFrom != null, x => x.ApprovedOn >= approvedFrom)
            .Where(approvedTo != null, x => x.ApprovedOn < approvedTo)
            .Where(status != null, x => x.Status == (short)status!)
            .Where(negativeAgentDividendCarryForward != null,
                x => x.NegativeAgentDividendCarryForward == negativeAgentDividendCarryForward)
            .Join(context.Agents,
                adr => adr.AgentId,
                a => a.Id,
                (adr, a) => new
                {
                    adr,
                    a.Username
                })
            .Where(!string.IsNullOrEmpty(agentUsername), x => EF.Functions.ILike(x.Username, $"%{agentUsername}%"))
            .Select(x =>
                new AgentDividendRecordModel(x.adr.Id,
                    x.adr.BrandId,
                    x.adr.AgentId,
                    (DistributeStatus)x.adr.Status,
                    x.adr.From,
                    x.adr.To,
                    x.adr.AgentDividendId,
                    x.Username,
                    x.adr.AgentDividendAmount,
                    x.adr.ActualAgentDividendAmount < 0 ? 0 : x.adr.ActualAgentDividendAmount,
                    x.adr.NegativeAgentDividendAmount,
                    x.adr.NegativeAgentDividendCarryForward,
                    x.adr.CreatedOn,
                    x.adr.UpdatedOn,
                    x.adr.ApprovedOn,
                    x.adr.Remark))
            .ToAsyncEnumerable();
    }

    public Task<AgentDividendRecordManuallyModel?> GetAgentDividendRecordAsync(Guid id,
        CancellationToken cancellationToken)
    {
        return context.AgentDividendRecords.Where(x => x.Id == id && x.ApprovedOn == null)
            .Join(context.Agents,
                adr => adr.AgentId,
                a => a.Id,
                (adr, a) => new
                {
                    adr.Id,
                    adr.BrandId,
                    adr.AgentId,
                    adr.AgentDividendId,
                    adr.AgentDividendAmount,
                    adr.ActualAgentDividendAmount,
                    adr.NegativeAgentDividendAmount,
                    adr.NegativeAgentDividendCarryForward,
                    adr.CreatedOn,
                    adr.UpdatedOn,
                    adr.ApprovedOn
                })
            .Select(x =>
                new AgentDividendRecordManuallyModel(x.Id,
                    x.BrandId,
                    x.AgentId,
                    x.AgentDividendId,
                    x.AgentDividendAmount,
                    x.ActualAgentDividendAmount,
                    x.NegativeAgentDividendAmount,
                    x.NegativeAgentDividendCarryForward,
                    x.CreatedOn,
                    x.UpdatedOn,
                    x.ApprovedOn))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task CreateAgentDividendsAsync(BrandModel brand,
        IEnumerable<AgentDividendModel> agentDividends,
        IEnumerable<CreateAgentDividendRecordsModel> records,
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

        await context.AgentDividendRecords.AddRangeAsync(records
                .Select(x => new AgentDividendRecord
                {
                    Id = GuidExtensions.NewSequentialGuid(),
                    BrandId = brand.Id,
                    AgentId = x.AgentId,
                    Status = (short)x.Status,
                    AgentDividendId = x.AgentDividendId,
                    AgentDividendAmount = x.AgentDividendAmount,
                    ActualAgentDividendAmount = x.AgentDividendAmount + x.NegativeAgentDividendAmount,
                    NegativeAgentDividendAmount = x.NegativeAgentDividendAmount,
                    NegativeAgentDividendCarryForward = x.NegativeAgentDividendCarryForward,
                    CreatedOn = x.CreatedOn,
                    UpdatedOn = x.UpdatedOn,
                    ApprovedOn = x.ApprovedOn,
                    SentByUsername = x.SentByUsername
                }),
            cancellationToken);

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

        await context.AgentProductFees.Where(x => x.BrandId == brand.Id)
            .ExecuteUpdateAsync(s =>
                    s.SetProperty(e => e.PreviousCardGamePercentage, e => e.CurrentCardGamePercentage)
                        .SetProperty(e => e.PreviousLiveDealerPercentage, e => e.CurrentLiveDealerPercentage)
                        .SetProperty(e => e.PreviousLotteryPercentage, e => e.CurrentLotteryPercentage)
                        .SetProperty(e => e.PreviousSlotPercentage, e => e.CurrentSlotPercentage)
                        .SetProperty(e => e.PreviousSportsPercentage, e => e.CurrentSportsPercentage)
                        .SetProperty(e => e.PreviousVirtualSportsPercentage, e => e.CurrentVirtualSportsPercentage)
                        .SetProperty(e => e.CurrentCardGamePercentage, e => e.NextCardGamePercentage)
                        .SetProperty(e => e.CurrentLiveDealerPercentage, e => e.NextLiveDealerPercentage)
                        .SetProperty(e => e.CurrentLotteryPercentage, e => e.NextLotteryPercentage)
                        .SetProperty(e => e.CurrentSlotPercentage, e => e.NextSlotPercentage)
                        .SetProperty(e => e.CurrentSportsPercentage, e => e.NextSportsPercentage)
                        .SetProperty(e => e.CurrentVirtualSportsPercentage, e => e.NextVirtualSportsPercentage),
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

    public async Task UpdateSentOnByManualSendAsync(int agentDividendId,
        DistributeStatus status,
        string sendByUsername,
        string? remark,
        CancellationToken cancellationToken)
    {
        await context.AgentDividends
            .Where(x => x.Id == agentDividendId || x.ReferencedAgentDividendId == agentDividendId)
            .ExecuteUpdateAsync(s =>
                    s.SetProperty(e => e.SentOn, DateTime.UtcNow),
                cancellationToken);

        await context.AgentDividendRecords.Where(x => x.AgentDividendId == agentDividendId)
            .ExecuteUpdateAsync(s =>
                    s.SetProperty(e => e.ApprovedOn, DateTime.UtcNow)
                        .SetProperty(e => e.Status, (short)status)
                        .SetProperty(e => e.SentByUsername, sendByUsername)
                        .SetProperty(e => e.Remark, remark),
                cancellationToken);

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateSentOnByAutoSendAsync(int agentDividendId, string sendByUsername,
        CancellationToken cancellationToken)
    {
        var agentDividendIds = await context.AgentDividends
            .Where(x => x.Id == agentDividendId || x.ReferencedAgentDividendId == agentDividendId)
            .Select(x => x.Id)
            .ToArrayAsync(cancellationToken);

        await context.AgentDividends
            .Where(x => x.Id == agentDividendId || x.ReferencedAgentDividendId == agentDividendId)
            .ExecuteUpdateAsync(s =>
                    s.SetProperty(e => e.SentOn, DateTime.UtcNow),
                cancellationToken);

        await context.AgentDividendRecords.Where(x => agentDividendIds.Contains(x.AgentDividendId))
            .ExecuteUpdateAsync(s =>
                    s.SetProperty(e => e.ApprovedOn, DateTime.UtcNow)
                        .SetProperty(e => e.SentByUsername, sendByUsername),
                cancellationToken);

        await context.SaveChangesAsync(cancellationToken);
    }

    public void RemoveAllMemoryCache(CancellationToken cancellationToken)
    {
        memoryCacheKeyMapper.ClearAll();
    }

    public async Task ClearNegativeCommissionAsync(Guid agentId, Guid clearByUserId,
        CancellationToken cancellationToken)
    {
        var commissions = context.AgentDividends
            .Where(x => x.AgentId == agentId && x.SentOn == null)
            .ToAsyncEnumerable();

        await context.AddRangeAsync(
                commissions.Select(x => new AgentClearCommissionRecord
                    {
                        Id = GuidExtensions.NewSequentialGuid(),
                        AgentDividendId = x.Id,
                        CommissionAmount = x.AgentDividendAmount,
                        ClearByUserId = clearByUserId,
                        CreatedOn = DateTime.UtcNow
                    })
                    .ToEnumerable(),
                cancellationToken)
            .ConfigureAwait(false);

        await commissions.ForEachAsync(x => x.SentOn = DateTime.UtcNow, cancellationToken)
            .ConfigureAwait(false);

        context.AgentDividends.UpdateRange(commissions.ToEnumerable());

        _ = await context.SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public Task<int> GetCompletePlayerAggregateCountAsync(DateTime from, DateTime to, CancellationToken cancellationToken)
    {
        return context.AggregateExecutionTimes
            .Where(x => x.From >= from && x.To <= to && x.AggregateType == (short)AggregateType.Player)
            .CountAsync(cancellationToken);
    }

    private async ValueTask CreateAgentDividendsAsync(IEnumerable<AgentDividendModel> models)
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
                SentOn = model.SentOn?.UtcDateTime,
                BeforeDividendAmount = model.BeforeDividendAmount,
                ReferencedAgentDividendId = model.ReferencedAgentDividendId,
                ActiveNumber = model.ActiveNumber,
                NegativeAgentDividendCarryForward = model.NegativeAgentDividendCarryForward,
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
                            Dividend = x.Dividend,
                            AgentDividendSnapshotId = x.AgentDividendSnapshotId
                        })
                    .ToArray()
            })
        );

        await context.AgentDividendProductFees.AddRangeAsync(models.Where(x => x.AgentDividendProductFee != null)
            .Select(x => new AgentDividendProductFee
            {
                BrandId = x.BrandId,
                AgentId = x.AgentId,
                From = x.From.UtcDateTime,
                To = x.To.UtcDateTime,
                AgentDividendId = x.AgentDividendProductFee!.AgentDividendId,
                CardGameWinLossAmount = x.AgentDividendProductFee!.CardGameWinLossAmount,
                CardGamePercentage = x.AgentDividendProductFee!.CurrentCardGamePercentage,
                LiveDealerWinLossAmount = x.AgentDividendProductFee!.LiveDealerWinLossAmount,
                LiveDealerPercentage = x.AgentDividendProductFee!.CurrentLiveDealerPercentage,
                LotteryWinLossAmount = x.AgentDividendProductFee!.LotteryWinLossAmount,
                LotteryPercentage = x.AgentDividendProductFee!.CurrentLotteryPercentage,
                SlotWinLossAmount = x.AgentDividendProductFee!.SlotWinLossAmount,
                SlotPercentage = x.AgentDividendProductFee!.CurrentSlotPercentage,
                SportsWinLossAmount = x.AgentDividendProductFee!.SportsWinLossAmount,
                SportsPercentage = x.AgentDividendProductFee!.CurrentSportsPercentage,
                VirtualSportsWinLossAmount = x.AgentDividendProductFee!.VirtualSportsWinLossAmount,
                VirtualSportsPercentage = x.AgentDividendProductFee!.CurrentVirtualSportsPercentage,
                CreatedOn = DateTime.UtcNow
            }));
    }
}