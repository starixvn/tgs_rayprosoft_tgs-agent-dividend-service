using MediatR;

namespace Tgs.Agent.Dividend.Domain.Models;

public record GetNoSendDividendRequest(IEnumerable<AgentDividendModel> AgentDividends)
    : IRequest<NoSendDividendResponse>;