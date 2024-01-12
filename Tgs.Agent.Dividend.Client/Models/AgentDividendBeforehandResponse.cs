namespace Tgs.Agent.Dividend.Client.Models;

public class AgentDividendBeforehandResponse
{
    public AgentDividendBeforehand Previous { get; set; } = null!;
    public AgentDividendBeforehand Current { get; set; } = null!;
}