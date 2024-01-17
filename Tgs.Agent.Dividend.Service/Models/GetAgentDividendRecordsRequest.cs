namespace Tgs.Agent.Dividend.Service.Models;

public class GetAgentDividendRecordsRequest
{
    public Guid BrandId { get; set; }
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public string AgentUsername { get; set; } = null!;
}