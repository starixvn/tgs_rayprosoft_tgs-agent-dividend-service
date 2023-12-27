namespace Tgs.Agent.Dividend.Domain.Models
{
    public record BrandContractCondition(
        int Rank,
        int ActiveNumber,
        decimal DividendPercentage,
        decimal NetProfit,
        decimal WinLossAmount
        );
}
