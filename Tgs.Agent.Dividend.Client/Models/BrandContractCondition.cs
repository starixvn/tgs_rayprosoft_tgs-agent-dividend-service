using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tgs.Agent.Dividend.Client.Models
{
    public record BrandContractCondition(
        int Rank,
        int ActiveNumber,
        decimal DividendPercentage,
        decimal NetProfit,
        decimal WinLossAmount
        );
}
