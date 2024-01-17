using MediatR;

namespace Tgs.Agent.Dividend.Domain.Models.BrandContract;

public record GetAgentDividendRecordsRequest(Guid BrandId, DateTime From, DateTime To, string? AgentUsername)
    : IStreamRequest<AgentDividendRecordModel>;