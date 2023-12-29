using MediatR;
using Tgs.Agent.Dividend.Domain.Enums;

namespace Tgs.Agent.Dividend.Domain.Models.BrandContract;

public record CreateBrandContractRequest(
    Guid BrandId,
    string Name,
    short ContractDividendType,
    decimal GradeIntervalLimit,
    decimal ValidBetAmount,
    decimal DepositAmount,
    int BetDays,
    string EditorUsername,
    ContractGameType[]? GameTypes,
    IEnumerable<CreateBrandContractCondition> Conditions) : IRequest;