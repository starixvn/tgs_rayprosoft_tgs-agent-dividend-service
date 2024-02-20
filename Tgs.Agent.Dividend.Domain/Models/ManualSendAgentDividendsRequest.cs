
namespace Tgs.Agent.Dividend.Domain.Models;

public record ManualSendAgentDividendsRequest
{
    public Guid[] AgentDividendRecordIds { get; set; } = null!;
    public bool Approve { get; set; }
    public string EditorUsername { get; set; } = null!;
    public string? Remark { get; set; }
}