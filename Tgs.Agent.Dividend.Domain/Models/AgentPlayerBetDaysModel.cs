namespace Tgs.Agent.Dividend.Domain.Models;

public class AgentPlayerBetDaysModel
{
    public Guid AgentId { get; set; }
    public Guid BrandId { get; set; }
    public string PlayerCode { get; set; } = null!;
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public int DayCount { get; set; }
}