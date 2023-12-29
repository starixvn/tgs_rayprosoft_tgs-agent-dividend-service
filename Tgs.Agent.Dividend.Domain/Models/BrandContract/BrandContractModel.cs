namespace Tgs.Agent.Dividend.Domain.Models.BrandContract;

public record BrandContractModel(
    Guid Id,
    Guid BrandId,
    string Name,
    short ContractDividendType,
    decimal GradeIntervalLimit,
    decimal ValidBetAmount,
    decimal DepositAmount,
    int BetDays,
    int[] GameTypes,
    IEnumerable<BrandContractConditionModel> Conditions);