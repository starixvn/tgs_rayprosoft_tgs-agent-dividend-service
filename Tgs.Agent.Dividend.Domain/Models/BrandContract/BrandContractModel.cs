using Tgs.Agent.Dividend.Domain.Enums;

namespace Tgs.Agent.Dividend.Domain.Models.BrandContract;

public record BrandContractModel(
    Guid Id,
    Guid BrandId,
    string Name,
    ContractDividendType ContractDividendType,
    decimal ValidBetAmount,
    decimal DepositAmount,
    int BetDays,
    int? SettlementDay,
    SettlementIntervalType SettlementIntervalType,
IEnumerable<BrandContractConditionModel> Conditions);