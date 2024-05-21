namespace Tgs.Agent.Dividend.Client.Models;

public class AgentDividendBeforehandResponse
{
    public IEnumerable<AgentDividendBeforehand> Previous { get; set; } = null!;
    public IEnumerable<AgentDividendBeforehand> Current { get; set; } = null!;
}