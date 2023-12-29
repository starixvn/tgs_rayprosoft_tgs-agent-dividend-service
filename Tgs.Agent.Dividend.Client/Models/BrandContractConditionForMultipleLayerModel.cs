namespace Tgs.Agent.Dividend.Client.Models;

public record BrandContractConditionForMultipleLayerModel(
    int Rank,
    int ActiveNumber,
    decimal DividendPercentage,
    decimal NetProfit,
    decimal WinLossAmount
);