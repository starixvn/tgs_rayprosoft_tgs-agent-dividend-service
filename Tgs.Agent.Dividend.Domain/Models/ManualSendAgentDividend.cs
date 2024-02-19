using MediatR;

namespace Tgs.Agent.Dividend.Domain.Models;

public record ManualSendAgentDividend(Guid AgentDividendRecordId, string EditorUsername,string? Remark) : IRequest;