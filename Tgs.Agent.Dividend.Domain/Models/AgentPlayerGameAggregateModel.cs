namespace Tgs.Agent.Dividend.Domain.Models;

public class AgentPlayerGameAggregateModel
{
    public Guid AgentId { get; set; }
    public Guid BrandId { get; set; }
    public DateTimeOffset From { get; set; }
    public DateTimeOffset To { get; set; }
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
}