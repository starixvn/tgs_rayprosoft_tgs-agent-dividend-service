using Tgs.Agent.Dividend.Client.Enums;

namespace Tgs.Agent.Dividend.Client.Models;

public record GetAgentDividendRecords(
    Guid BrandId, 
    DistributeStatus? Status, 
    DateTime? From, 
    DateTime? To, 
    DateTime? ApprovedFrom, 
    DateTime? ApprovedTo, 
    string? AgentUsername,
    bool? NegativeAgentDividendCarryForward);