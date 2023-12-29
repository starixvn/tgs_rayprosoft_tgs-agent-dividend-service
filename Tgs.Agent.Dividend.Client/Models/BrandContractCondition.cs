namespace Tgs.Agent.Dividend.Client.Models;

public record BrandContractCondition(
    int Rank,
    int ActiveNumber,
    decimal DividendPercentage,
    decimal NetProfit,
    decimal WinLossAmount
);