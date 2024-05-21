namespace Tgs.Agent.Dividend.Domain.Models;

public class AgentDividendBeforehandResponse
{
    public IEnumerable<AgentDividendBeforehand> Previous { get; set; } = null!;
    public IEnumerable<AgentDividendBeforehand> Current { get; set; } = null!;
}