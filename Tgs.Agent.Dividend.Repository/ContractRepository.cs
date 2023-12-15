using Tgs.Agent.Dividend.Domain.Interfaces.Repositories;
using Tgs.Agent.Dividend.Domain.Models;
using Tgs.Agent.Dividend.Repository.Entities;

namespace Tgs.Agent.Dividend.Repository;

internal class ContractRepository(tgsContext context) : IContractRepository
{
    public IAsyncEnumerable<AgentDividendContractModel> GetAgentDividendContractsAsync(Guid brandId)
    {
        return context.AgentDividendContracts.Where(x => x.BrandId == brandId)
            .Select(x => new AgentDividendContractModel
            {
                Id = x.Id,
                Name = x.Name,
                BrandId = x.BrandId,
                AgentId = x.AgentId,
                ContractDividendType = x.ContractDividendType,
                ContractStatus = x.ContractStatus,
                ValidBetAmount = x.ValidBetAmount,
                DepositAmount = x.DepositAmount,
                BetDays = x.BetDays
            })
            .ToAsyncEnumerable();
    }

    public IAsyncEnumerable<AgentDividendContractPercentageModel> GetAgentDividendContractConditionsAsync(
        Guid agentDividendContractId,
        CancellationToken cancellationToken)
    {
        return context.AgentDividendConditions
            .Where(x => x.AgentDividendContractId == agentDividendContractId)
            .Select(x => new AgentDividendContractPercentageModel
            {
                AgentDividendContractId = x.AgentDividendContractId,
                Rank = x.Rank,
                ValidNumber = x.ActiveNumber,
                WinLossAmount = x.WinLossAmount,
                NetProfit = x.NetProfit,
                Percentage = x.DividendPercentage
            })
            .ToAsyncEnumerable();
    }
}