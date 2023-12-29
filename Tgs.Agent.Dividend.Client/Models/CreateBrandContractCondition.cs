namespace Tgs.Agent.Dividend.Client.Models;

public record CreateBrandContractCondition(
    int Rank,
    int ActiveNumber,
    decimal DividendPercentage,
    decimal NetProfit,
    decimal WinLossAmount
);