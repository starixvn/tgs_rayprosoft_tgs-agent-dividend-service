namespace Tgs.Agent.Dividend.Domain.Models.BrandContract;

public record BrandContractConditionModel(
    int Rank,
    int ActiveNumber,
    decimal DividendPercentage,
    decimal NetProfit,
    decimal WinLossAmount
);