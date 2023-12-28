namespace Tgs.Agent.Dividend.Client.Models;

public class AgentDividendContract
{
    public Guid Id { get; set; }
    public Guid BrandId { get; set; }
    public Guid AgentId { get; set; }
    public string Name { get; set; } = null!;
    public short ContractDividendType { get; set; }
    public short ContractStatus { get; set; }
    public decimal ValidBetAmount { get; set; }
    public decimal DepositAmount { get; set; }
    public int BetDays { get; set; }
    public IEnumerable<AgentDividendContractCondition> Conditions { get; set; } = null!;
}