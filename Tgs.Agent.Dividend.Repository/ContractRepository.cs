using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using Tgs.Agent.Dividend.Domain;
using Tgs.Agent.Dividend.Domain.Enums;
using Tgs.Agent.Dividend.Domain.Interfaces.Repositories;
using Tgs.Agent.Dividend.Domain.Models;
using Tgs.Agent.Dividend.Domain.Models.AgentContract;
using Tgs.Agent.Dividend.Domain.Models.BrandContract;
using Tgs.Agent.Dividend.Repository.Entities;

namespace Tgs.Agent.Dividend.Repository;

internal class ContractRepository(tgsContext context) : IContractRepository
{
    public IAsyncEnumerable<AgentDividendContractModel> GetBrandAgentDividendContractsAsync(Guid brandId)
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

    public IAsyncEnumerable<AgentDividendContractConditionModel> GetAgentDividendContractConditionsAsync(
        Guid agentDividendContractId,
        CancellationToken cancellationToken)
    {
        return context.AgentDividendConditions
            .Where(x => x.ContractId == agentDividendContractId)
            .Select(x => new AgentDividendContractConditionModel
            {
                AgentDividendContractId = x.ContractId,
                Rank = x.Rank,
                ValidNumber = x.ActiveNumber,
                WinLossAmount = x.WinLossAmount,
                NetProfit = x.NetProfit,
                Percentage = x.DividendPercentage
            })
            .ToAsyncEnumerable();
    }

    public IAsyncEnumerable<BrandContractModel> GetBrandContractsAsync(Guid brandId)
    {
        return context.BrandContracts.Where(x => x.BrandId == brandId)
            .Select(x => new BrandContractModel(
                x.Id,
                x.BrandId,
                x.Name,
                (ContractDividendType)x.ContractDividendType,
                x.GradeIntervalLimit,
                x.ValidBetAmount,
                x.DepositAmount,
                x.BetDays,
                x.GameTypes.Select(g => int.Parse(g)).ToArray(),
                x.SettlementDay,
                (SettlementIntervalType)x.SettlementIntervalType,
                x.BrandContractConditions.Select(b =>
                    new BrandContractConditionModel(
                        b.Rank,
                        b.ActiveNumber,
                        b.DividendPercentage,
                        b.NetProfit,
                        b.WinLossAmount))))
            .ToAsyncEnumerable();
    }

    public async IAsyncEnumerable<AgentDividendContractViewModel> GetAgentDividendContractsAsync(Guid agentId,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var contracts = await context.AgentDividendContracts.Where(x => x.AgentId == agentId)
            .ToArrayAsync(cancellationToken);
        var conditions = await context.AgentDividendConditions.Where(x => x.AgentId == agentId)
            .ToArrayAsync(cancellationToken);

        var models = contracts.Select(x => new AgentDividendContractViewModel
        {
            Id = x.Id,
            BrandId = x.BrandId,
            AgentId = x.AgentId,
            Name = x.Name,
            ContractDividendType = x.ContractDividendType,
            ContractStatus = x.ContractStatus,
            ValidBetAmount = x.ValidBetAmount,
            DepositAmount = x.DepositAmount,
            BetDays = x.BetDays,
            Conditions = conditions.Where(c => c.ContractId == x.Id)
                .Select(s => new AgentDividendContractConditionViewModel
                {
                    Rank = s.Rank,
                    ValidNumber = s.ActiveNumber,
                    WinLossAmount = s.WinLossAmount,
                    NetProfit = s.NetProfit,
                    Percentage = s.DividendPercentage,
                    AgentDividendContractId = x.Id
                })
        });

        foreach (var agentDividendContractViewModel in models) yield return agentDividendContractViewModel;
    }

    public async Task CreateAgentContractAsync(CreateAgentContractRequest request, CancellationToken cancellationToken)
    {
        var contractId = Guid.NewGuid();

        var brandContract = await context.BrandContracts
            .Include(x => x.BrandContractConditions)
            .FirstAsync(x => x.Id == request.BrandContractId, cancellationToken)
            .ConfigureAwait(false);

        var contract = new AgentDividendContract
        {
            Id = contractId,
            BrandId = request.BrandId,
            AgentId = request.AgentId,
            DefaultBrandContractId = brandContract.Id,
            Name = brandContract.Name,
            GradeIntervalLimit = brandContract.GradeIntervalLimit,
            ContractDividendType = brandContract.ContractDividendType,
            ContractStatus = (short)ContractStatus.Enabled,
            ValidBetAmount = brandContract.ValidBetAmount,
            DepositAmount = brandContract.DepositAmount,
            BetDays = brandContract.BetDays,
            GameTypes = brandContract.GameTypes,
            CreatedByUsername = request.EditorUsername,
            UpdatedByUsername = request.EditorUsername,
            CreatedOn = DateTime.UtcNow,
            UpdatedOn = DateTime.UtcNow
        };

        var conditions = brandContract.BrandContractConditions.Select(x => new AgentDividendCondition
        {
            ContractId = contractId,
            Rank = x.Rank,
            AgentId = request.AgentId,
            NetProfit = x.NetProfit,
            ActiveNumber = x.ActiveNumber,
            WinLossAmount = x.WinLossAmount
        });

        var now = DateTimeOffset.Now.ToOffset(TimeSpan.FromHours(8));

        var (currentFrom, currentTo) = DateTimeOffsetExtensions.GetInterval((SettlementIntervalType)brandContract.SettlementIntervalType,
            new DateTimeOffset(now.Year, now.Month, now.Day, 0, 0, 0, now.Offset),
            brandContract.SettlementDay);

        var (nextFrom, nextTo) = DateTimeOffsetExtensions.GetInterval((SettlementIntervalType)brandContract.SettlementIntervalType,
            currentTo.AddDays(1),
            brandContract.SettlementDay);

        _ = await context.AgentDividendContractTimeIntervals.AddAsync(new AgentDividendContractTimeInterval
        {
            PreviousSettlementFrom = currentFrom.UtcDateTime,
            PreviousSettlementTo = currentTo.UtcDateTime,
            CurrentSettlementFrom = currentFrom.UtcDateTime,
            CurrentSettlementTo = currentTo.UtcDateTime,
            NextSettlementFrom = nextFrom.UtcDateTime,
            NextSettlementTo = nextTo.UtcDateTime
        }, cancellationToken).ConfigureAwait(false);

        _ = await context.AgentDividendContracts.AddAsync(contract, cancellationToken).ConfigureAwait(false);

        await context.AgentDividendConditions.AddRangeAsync(conditions, cancellationToken).ConfigureAwait(false);

        _ = await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task CreateBrandContractAsync(CreateBrandContractRequest request, CancellationToken cancellationToken)
    {
        var id = Guid.NewGuid();

        var contract = new BrandContract
        {
            Id = id,
            BrandId = request.BrandId,
            Name = request.Name,
            GradeIntervalLimit = request.GradeIntervalLimit,
            ContractDividendType = request.ContractDividendType,
            ContractStatus = (short)ContractStatus.Enabled,
            ValidBetAmount = request.ValidBetAmount,
            DepositAmount = request.DepositAmount,
            BetDays = request.BetDays,
            GameTypes = request.GameTypes?.Select(x => ((int)x).ToString()).ToList() ?? [],
            CreatedByUsername = request.EditorUsername,
            UpdatedByUsername = request.EditorUsername,
            CreatedOn = DateTime.UtcNow,
            UpdatedOn = DateTime.UtcNow,
            BrandContractConditions = request.Conditions.Select(x => new BrandContractCondition
                {
                    BrandContractId = id,
                    Rank = x.Rank,
                    ActiveNumber = x.ActiveNumber,
                    NetProfit = x.NetProfit,
                    WinLossAmount = x.WinLossAmount,
                    DividendPercentage = x.DividendPercentage
                })
                .ToHashSet()
        };

        _ = await context.BrandContracts.AddAsync(contract, cancellationToken).ConfigureAwait(false);
        _ = await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateBrandContractConditionsForMultipleLayerAsync(
        UpdateBrandContractConditionForMultipleLayerRequest request,
        CancellationToken cancellationToken)
    {
        var brandContract = await context.BrandContracts
            .FirstAsync(x => x.BrandId == request.BrandId, cancellationToken)
            .ConfigureAwait(false);

        var brandAgentDividendConditions = request.Conditions.Select(x
            => new BrandAgentDividendCondition
            {
                BrandId = request.BrandId,
                Rank = x.Rank,
                ActiveNumber = x.ActiveNumber,
                NetProfit = x.NetProfit,
                DividendPercentage = x.DividendPercentage
            });

        var brandContractConditions = request.Conditions.Select(x
            => new BrandContractCondition
            {
                BrandContractId = brandContract.Id,
                Rank = x.Rank,
                ActiveNumber = x.ActiveNumber,
                NetProfit = x.NetProfit,
                DividendPercentage = x.DividendPercentage
            });

        await context.BrandContractConditions.Where(x => x.BrandContract.BrandId == request.BrandId)
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);

        await context.BrandAgentDividendConditions.Where(x => x.BrandId == request.BrandId)
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);

        await context.BrandAgentDividendConditions.AddRangeAsync(brandAgentDividendConditions, cancellationToken)
            .ConfigureAwait(false);

        await context.BrandContractConditions.AddRangeAsync(brandContractConditions, cancellationToken)
            .ConfigureAwait(false);

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task CreateAgentContractSnapshotAsync(Guid id, string data, CancellationToken cancellationToken)
    {
        await context.AgentDividendSnapshots.AddAsync(new AgentDividendSnapshot
                {
                    Id = id,
                    Data = data,
                    CreatedOn = DateTime.UtcNow
                },
                cancellationToken)
            .ConfigureAwait(false);

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}