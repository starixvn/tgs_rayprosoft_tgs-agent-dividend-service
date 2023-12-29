namespace Tgs.Agent.Dividend.Domain.Models.BrandContract;

public record BrandContractConditionForMultipleLayerModel(
    int Rank,
    int ActiveNumber,
    decimal DividendPercentage,
    decimal NetProfit,
    decimal WinLossAmount
);