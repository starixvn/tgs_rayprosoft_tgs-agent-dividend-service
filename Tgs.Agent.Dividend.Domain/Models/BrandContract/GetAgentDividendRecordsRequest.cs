using MediatR;
using Tgs.Agent.Dividend.Domain.Enums;

namespace Tgs.Agent.Dividend.Domain.Models.BrandContract;

public record GetAgentDividendRecordsRequest(
    Guid BrandId,
    DistributeStatus? Status,
    DateTime? From,
    DateTime? To,
    DateTime? ApprovedFrom,
    DateTime? ApprovedTo,
    string? AgentUsername,
    bool? NegativeAgentDividendCarryForward)
    : IStreamRequest<AgentDividendRecordModel>;