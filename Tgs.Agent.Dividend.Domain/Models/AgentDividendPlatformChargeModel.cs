namespace Tgs.Agent.Dividend.Domain.Models;

public class AgentDividendPlatformChargeModel
{
    public int AgentDividendId { get; set; }
    public Guid AgentId { get; set; }
    public decimal PlatformChargePercentage { get; set; }
    public decimal WinLossAmount { get; set; }
}