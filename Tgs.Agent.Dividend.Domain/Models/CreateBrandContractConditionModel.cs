namespace Tgs.Agent.Dividend.Domain.Models
{
    public record CreateBrandContractCondition(
        int Rank,
        int ActiveNumber,
        decimal DividendPercentage,
        decimal NetProfit,
        decimal WinLossAmount
        );
}
