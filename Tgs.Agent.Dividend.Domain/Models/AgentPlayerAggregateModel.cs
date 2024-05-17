using Tgs.Agent.Dividend.Domain.Enums;

namespace Tgs.Agent.Dividend.Domain.Models;

public class AgentPlayerAggregateModel
{
    public int Id { get; set; }
    public Guid AgentId { get; set; }
    public Guid BrandId { get; set; }
    public DateTimeOffset From { get; set; }
    public DateTimeOffset To { get; set; }
    public bool IsPlayerAgent { get; set; }
    public decimal PromotionAmount { get; set; }
    public decimal RebateAmount { get; set; }
    public decimal DepositAmount { get; set; }
    public decimal WithdrawalAmount { get; set; }
    public decimal CompanyAdjustAmount { get; set; }
    public decimal AgentAdjustAmount { get; set; }
    public decimal BonusAdjustAmount { get; set; }
    public decimal BonusAmount { get; set; }
    public decimal BetAmount { get; set; }
    public decimal ValidBetAmount { get; set; }
    public decimal OriginalTurnover { get; set; }
    public decimal Turnover { get; set; }
    public decimal WinLossAmount { get; set; }
    public decimal NoIncludeNetProfitCompanyAdjustAmount { get; set; }
    public decimal NoIncludeNetProfitAgentAdjustAmount { get; set; }
    public decimal NoIncludeNetProfitBonusAdjustAmount { get; set; }
    public decimal AgentDividendPercentage { get; set; }
    public string? AgentDividendPercentageTreeData { get; set; }
    public decimal LiveDealerWinLoss { get; set; }
    public decimal SlotWinLoss { get; set; }
    public decimal FishingWinLoss { get; set; }
    public decimal ArcadeWinLoss { get; set; }
    public decimal TableGameWinLoss { get; set; }
    public decimal LotteryWinLoss { get; set; }
    public decimal CardGameWinLoss { get; set; }
    public decimal SportsWinLoss { get; set; }
    public decimal ESportsWinLoss { get; set; }
    public decimal VirtualSportsWinLoss { get; set; }
    public decimal AgentDividendAmount { get; set; }
    public int ActiveNumber { get; set; }
    public AgentDividendPlatformChargeModel[]? AgentDividendPlatformCharges { get; set; }
    public AgentDividendProductFeeModel[]? AgentDividendProductFees { get; set; }

    public decimal NetProfit()
    {
        return WinLossAmount -
               (BonusAdjustAmount -
                NoIncludeNetProfitBonusAdjustAmount) -
               BonusAmount;
    }

    public decimal ProductFeeAmount()
    {
        var cardGameAmount =
            AgentDividendProductFees?.Where(w => w.GameGroupType == GameGroupType.CardGame).Sum(s => s.WinLossAmount) <=
            0
                ? 0M
                : AgentDividendProductFees?.Where(w => w.GameGroupType == GameGroupType.CardGame)
                    .Sum(s => s.WinLossAmount * s.ProductFeePercentage / 100) ?? 0M;

        var liveDealerAmount =
            AgentDividendProductFees?.Where(w => w.GameGroupType == GameGroupType.LiveDealer)
                .Sum(s => s.WinLossAmount) <= 0
                ? 0M
                : AgentDividendProductFees?.Where(w => w.GameGroupType == GameGroupType.LiveDealer)
                    .Sum(s => s.WinLossAmount * s.ProductFeePercentage / 100) ?? 0M;

        var lotteryAmount =
            AgentDividendProductFees?.Where(w => w.GameGroupType == GameGroupType.Lottery).Sum(s => s.WinLossAmount) <=
            0
                ? 0M
                : AgentDividendProductFees?.Where(w => w.GameGroupType == GameGroupType.Lottery)
                    .Sum(s => s.WinLossAmount * s.ProductFeePercentage / 100) ?? 0M;

        var sportsAmount =
            AgentDividendProductFees?.Where(w => w.GameGroupType == GameGroupType.Sportsbook)
                .Sum(s => s.WinLossAmount) <= 0
                ? 0M
                : AgentDividendProductFees?.Where(w => w.GameGroupType == GameGroupType.Sportsbook)
                    .Sum(s => s.WinLossAmount * s.ProductFeePercentage / 100) ?? 0M;

        var slotAmount =
            AgentDividendProductFees?.Where(w => w.GameGroupType == GameGroupType.Slot).Sum(s => s.WinLossAmount) <= 0
                ? 0M
                : AgentDividendProductFees?.Where(w => w.GameGroupType == GameGroupType.Slot)
                    .Sum(s => s.WinLossAmount * s.ProductFeePercentage / 100) ?? 0M;

        var virtualSportsAmount = AgentDividendProductFees?.Where(w => w.GameGroupType == GameGroupType.VirtualSports)
            .Sum(s => s.WinLossAmount) <= 0
            ? 0M
            : AgentDividendProductFees?.Where(w => w.GameType == GameType.VirtualSports)
                .Sum(s => s.WinLossAmount * s.ProductFeePercentage / 100) ?? 0M;

        return cardGameAmount + liveDealerAmount + lotteryAmount + sportsAmount + slotAmount + virtualSportsAmount;
    }

    public decimal PlatformChargeAmount()
    {
        return AgentDividendPlatformCharges?.Sum(x => x.WinLossAmount) <= 0
            ? 0M
            : AgentDividendPlatformCharges?.Sum(x => x.PlatformChargePercentage * x.WinLossAmount / 100) ?? 0M;
    }
}