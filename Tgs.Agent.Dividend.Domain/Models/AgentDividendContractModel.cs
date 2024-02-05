using Tgs.Agent.Dividend.Domain.Enums;

namespace Tgs.Agent.Dividend.Domain.Models;

public class AgentDividendContractModel
{
    public Guid Id { get; set; }
    public Guid BrandId { get; set; }
    public Guid AgentId { get; set; }
    public NegativeAgentDividendResponsibilityType NegativeAgentDividendResponsibilityType { get; set; }
    public string Name { get; set; } = null!;
    public short ContractDividendType { get; set; }
    public short ContractStatus { get; set; }
    public decimal ValidBetAmount { get; set; }
    public decimal DepositAmount { get; set; }
    public int BetDays { get; set; }
    public decimal[] LevelPercentages { get; set; } = default!;
    public decimal DepositFeePercentage { get; set; }
    public decimal WithdrawFeePercentage { get; set; }
    public decimal SportsbookPercentage { get; set; }
    public decimal VirtualSportsPercentage { get; set; }
    public decimal LiveDealerPercentage { get; set; }
    public decimal CardGamePercentage { get; set; }
    public decimal LotteryPercentage { get; set; }
    public decimal SlotPercentage { get; set; }
    public bool AutoSend { get; set; }
}