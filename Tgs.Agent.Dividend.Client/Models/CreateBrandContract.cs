using Tgs.Agent.Dividend.Client.Enums;

namespace Tgs.Agent.Dividend.Client.Models;

public record CreateBrandContract(
    Guid BrandId,
    string Name,
    short ContractDividendType,
    decimal GradeIntervalLimit,
    decimal ValidBetAmount,
    decimal DepositAmount,
    int BetDays,
    string EditorUsername,
    ContractGameType[]? GameTypes,
    IEnumerable<CreateBrandContractCondition> Conditions);