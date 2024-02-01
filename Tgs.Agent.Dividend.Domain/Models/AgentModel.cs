using Microsoft.EntityFrameworkCore;

namespace Tgs.Agent.Dividend.Domain.Models;

public class AgentModel
{
    public Guid Id { get; set; }
    public Guid BrandId { get; set; }
    public string Username { get; set; } = null!;
    public Guid? ParentAgentId { get; set; }
    public LTree Path { get; set; }
    public decimal DividendPercentage { get; set; }
    public DateTime PreviousSettlementFrom { get; set; }
    public DateTime PreviousSettlementTo { get; set; }
    public DateTime CurrentSettlementFrom { get; set; }
    public DateTime CurrentSettlementTo { get; set; }
    public DateTime NextSettlementFrom { get; set; }
    public DateTime NextSettlementTo { get; set; }
}