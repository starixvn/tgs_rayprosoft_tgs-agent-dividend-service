using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tgs.Agent.Dividend.Client.Enums;

namespace Tgs.Agent.Dividend.Client.Models
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
