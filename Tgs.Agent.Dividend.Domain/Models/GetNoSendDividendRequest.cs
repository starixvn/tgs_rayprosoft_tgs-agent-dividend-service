using MediatR;

namespace Tgs.Agent.Dividend.Domain.Models;

public record GetNoSendDividendRequest(IEnumerable<CreateAgentDividendModel> AgentDividends) : IRequest<NoSendDividendResponse>;