using Tgs.Agent.Dividend.Domain.Enums;

namespace Tgs.Agent.Dividend.Domain.Models
{
    public record BrandContract(
        Guid Id,
        Guid BrandId,
        string Name,
        short ContractDividendType,
        decimal GradeIntervalLimit,
        decimal ValidBetAmount,
        decimal DepositAmount,
        int BetDays,
        ContractGameType[] GameTypes,
        IEnumerable<BrandContractCondition> Conditions);
}
