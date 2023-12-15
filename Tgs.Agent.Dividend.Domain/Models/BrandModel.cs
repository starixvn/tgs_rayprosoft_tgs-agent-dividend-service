namespace Tgs.Agent.Dividend.Domain.Models;

public class BrandModel
{
    public Guid Id { get; set; }
    public short SettlementIntervalType { get; set; }
    public DateTime PreviousSettlementFrom { get; set; }
    public DateTime PreviousSettlementTo { get; set; }
    public DateTime CurrentSettlementFrom { get; set; }
    public DateTime CurrentSettlementTo { get; set; }
    public DateTime NextSettlementFrom { get; set; }
    public DateTime NextSettlementTo { get; set; }
    public bool AutoSentAgentBonus { get; set; }
    public string Code { get; set; } = null!;
    public int? SettlementDay { get; set; }
    public decimal PlatformCharge { get; set; }
    public short AgentDividendType { get; set; }
    public bool CheckDividendActiveCount { get; set; }
}