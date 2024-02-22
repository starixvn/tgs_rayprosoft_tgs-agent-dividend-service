using Tgs.Agent.Dividend.Domain.Enums;

namespace Tgs.Agent.Dividend.Domain.Models;

public class CreateAgentDividendRecordsModel
{
    public Guid BrandId { get; set; }
    public Guid AgentId { get; set; }
    public int AgentDividendId { get; set; }
    public DistributeStatus Status { get; set; }
    public decimal AgentDividendAmount { get; set; }
    public decimal ActualAgentDividendAmount { get; set; }
    public decimal NegativeAgentDividendAmount { get; set; }
    public bool NegativeAgentDividendCarryForward { get; set; }
    public DateTime CreatedOn { get; set; }
    public DateTime UpdatedOn { get; set; }
    public DateTime? ApprovedOn { get; set; }
    public string? Remark { get; set; }
    public string? SentByUsername { get; set; }
}