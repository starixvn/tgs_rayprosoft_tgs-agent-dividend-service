
namespace Tgs.Agent.Dividend.Domain.Models;

public record ManualSendAgentDividendsRequest
{
    public Guid[] AgentDividendRecordIds { get; set; } = null!;
    public string EditorUsername { get; set; } = null!;
    public string? Remark { get; set; }
}