namespace Tgs.Agent.Dividend.Domain.Models;

public record AgentDividendBeforehand(
    DateTime From,
    DateTime To,
    int ActiveNumber,
    decimal CompanyWinLossAmount,
    decimal Bonus,
    decimal PlayerRebate,
    decimal DirectDividendAmount,
    decimal DividendPercentage,
    decimal TeamContributionAmount,
    decimal TotalDividendAmount,
    decimal BonusAdjustAmount,
    decimal AgentAdjustAmount,
    decimal ProductFeeAmount,
    decimal NegativeAgentDividendAmount,
    DateTime? SentOn);