namespace Tgs.Agent.Dividend.Client.Models;

public record AgentDividendBeforehand(
    int ActiveNumber,
    decimal CompanyWinLossAmount, 
    decimal Bonus,
    decimal PlayerRebate,
    decimal SettledProfitLoss,
    decimal PeriodProfitLoss,
    decimal DepositFee,
    decimal PlatformCharge,
    decimal DividendAmount,
    decimal DividendPercentage,
    decimal DownLineContributionAmount,
    decimal TotalDividendAmount,
    DateTime? SentOn);