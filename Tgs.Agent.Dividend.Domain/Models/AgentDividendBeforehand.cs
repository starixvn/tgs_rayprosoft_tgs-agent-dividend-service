namespace Tgs.Agent.Dividend.Domain.Models;

public record AgentDividendBeforehand(
    int ActiveNumber,
    decimal CompanyWinLossAmount,
    decimal Bonus,
    decimal PlayerRebate,
    decimal SettledProfitLoss,
    decimal PeriodProfitLoss,
    decimal PlatformCharge,
    decimal DividendAmount,
    decimal DividendPercentage,
    decimal DownLineContributionAmount,
    decimal TotalDividendAmount,
    DateTime? SentOn);