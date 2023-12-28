namespace Tgs.Agent.Dividend.Client.Models;

public class AgentDividendContractCondition
{
    public int Rank { get; set; }
    public int ValidNumber { get; set; }
    public decimal WinLossAmount { get; set; }
    public decimal NetProfit { get; set; }
    public decimal Percentage { get; set; }
    public Guid AgentDividendContractId { get; set; }
}