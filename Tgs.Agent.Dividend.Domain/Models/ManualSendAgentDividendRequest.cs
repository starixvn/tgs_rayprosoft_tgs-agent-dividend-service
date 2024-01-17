using MediatR;

namespace Tgs.Agent.Dividend.Domain.Models;

public record ManualSendAgentDividendRequest(Guid AgentDividendRecordId, string EditorUsername) : IRequest;