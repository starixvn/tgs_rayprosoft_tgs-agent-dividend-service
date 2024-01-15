namespace Tgs.Agent.Dividend.Domain.Models;

public class AgentDividendBeforehandResponse
{
    public AgentDividendBeforehand Previous { get; set; } = null!;
    public AgentDividendBeforehand Current { get; set; } = null!;
}