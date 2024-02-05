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

    public decimal PlatformChargeFee(decimal platformChargePercentage) =>
        WinLossAmount * platformChargePercentage / 100;

    public decimal DepositPaymentFee(decimal depositFeePercentage) => DepositAmount * depositFeePercentage / 100;
    public decimal WithdrawalPaymentFee(decimal withdrawFeePercentage) => WithdrawalAmount * withdrawFeePercentage / 100;

    public decimal SportsbookProductFee(decimal sportsbookProductPercentage) =>
        (SportsWinLoss + ESportsWinLoss) * sportsbookProductPercentage / 100;

    public decimal VirtualSportsProductFee(decimal virtualSportsProductPercentage) =>
        VirtualSportsWinLoss * virtualSportsProductPercentage / 100;

    public decimal LiveDealerProductFee(decimal liveDealerProductPercentage) =>
        LiveDealerWinLoss * liveDealerProductPercentage / 100;

    public decimal CardGameProductFee(decimal cardGameProductPercentage) =>
        CardGameWinLoss * cardGameProductPercentage / 100;

    public decimal LotteryProductFee(decimal lotteryProductPercentage) =>
        LotteryWinLoss * lotteryProductPercentage / 100;

    public decimal SlotProductFee(decimal slotProductPercentage) =>
        (FishingWinLoss + ArcadeWinLoss + SlotWinLoss + TableGameWinLoss) * slotProductPercentage / 100;

    public decimal NetProfit(decimal platformChargePercentage,
        decimal depositFeePercentage,
        decimal withdrawalFeePercentage,
        decimal sportsbookProductPercentage,
        decimal virtualSportsProductPercentage,
        decimal liveDealerProductPercentage,
        decimal cardGameProductPercentage,
        decimal lotteryProductPercentage,
        decimal slotProductPercentage)
    {
        return WinLossAmount -
               (BonusAdjustAmount -
                NoIncludeNetProfitBonusAdjustAmount) -
               BonusAmount -
               PlatformChargeFee(platformChargePercentage) -
               DepositPaymentFee(depositFeePercentage) -
               WithdrawalPaymentFee(withdrawalFeePercentage) -
               SportsbookProductFee(sportsbookProductPercentage) -
               VirtualSportsProductFee(virtualSportsProductPercentage) -
               LiveDealerProductFee(liveDealerProductPercentage) -
               CardGameProductFee(cardGameProductPercentage) -
               LotteryProductFee(lotteryProductPercentage) -
               SlotProductFee(slotProductPercentage);
    }
}