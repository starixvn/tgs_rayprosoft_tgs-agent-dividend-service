namespace Tgs.Agent.Dividend.Domain.Models;

public class NoSendDividendResponse
{
    public IEnumerable<AgentDividendModel> Model { get; set; } = null!;
    public IDictionary<int, int> NegativeDividendsIds { get; set; } = null!;
}