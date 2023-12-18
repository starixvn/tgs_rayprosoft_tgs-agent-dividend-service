namespace Tgs.Agent.Dividend.Domain.Models;

public class AgentConditionModel
{
    public decimal ValidNumber { get; set; }
    public decimal BonusAmount { get; set; }
    public decimal BonusAdjustAmount { get; set; }
    public decimal WinLossAmount { get; set; }
    public decimal NetProfit { get; set; }
}