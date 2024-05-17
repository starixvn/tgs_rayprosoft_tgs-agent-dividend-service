using MediatR;

namespace Tgs.Agent.Dividend.Domain.Models;

public record ManualSendAgentDividend(Guid AgentDividendRecordId, bool Approve, string EditorUsername, string? Remark)
    : IRequest;