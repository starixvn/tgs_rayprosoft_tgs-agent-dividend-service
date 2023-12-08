namespace Tgs.Agent.Dividend.Domain.Models;

public class AgentDividendContractValidNumberModel
{
    public Guid Id { get; set; }

    public decimal ValidBetAmount { get; set; }

    public decimal DepositAmount { get; set; }

    public int BetDays { get; set; }

    public Guid AgentDividendContractId { get; set; }
}