using Tgs.Agent.Dividend.Domain.Enums;

namespace Tgs.Agent.Dividend.Domain.Models;

public class AgentDividendProductFeeModel
{
    public int AgentDividendId { get; set; }
    public Guid AgentId { get; set; }
    public decimal CurrentCardGamePercentage { get; set; }
    public decimal CurrentLiveDealerPercentage { get; set; }
    public decimal CurrentLotteryPercentage { get; set; }
    public decimal CurrentSlotPercentage { get; set; }
    public decimal CurrentSportsPercentage { get; set; }
    public decimal CurrentVirtualSportsPercentage { get; set; }
    public decimal CardGameWinLossAmount { get; set; }
    public decimal LiveDealerWinLossAmount { get; set; }
    public decimal LotteryWinLossAmount { get; set; }
    public decimal SlotWinLossAmount { get; set; }
    public decimal SportsWinLossAmount { get; set; }
    public decimal VirtualSportsWinLossAmount { get; set; }
}