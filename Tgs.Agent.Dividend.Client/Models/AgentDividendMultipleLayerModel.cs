namespace Tgs.Agent.Dividend.Client.Models;

public class AgentDividendMultipleLayerModel
{
    public int AgentDividendId { get; set; }
    public Guid AgentId { get; set; }
    public Guid DividendProviderAgentId { get; set; }
    public Guid AgentDividendSnapshotId { get; set; }
    public int DividendProviderId { get; set; }
    public decimal AgentDividendPercentage { get; set; }
    public decimal ActualDividendPercentage { get; set; }
    public string Layer { get; set; } = default!;
    public decimal AgentCoveredBonus { get; set; }
    public decimal AgentCoveredBonusAdjust { get; set; }
    public decimal Dividend { get; set; }
}