using System;
using System.Reflection;
using System.Text.Json;
using AutoFixture;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NUnit.Framework.Internal.Execution;
using Tgs.Agent.Dividend.Core;
using Tgs.Agent.Dividend.Core.Workers;
using Tgs.Agent.Dividend.Domain.Enums;
using Tgs.Agent.Dividend.Domain.Events;
using Tgs.Agent.Dividend.Domain.Interfaces;
using Tgs.Agent.Dividend.Domain.Interfaces.Repositories;
using Tgs.Agent.Dividend.Domain.Models;
using Tgs.Agent.Dividend.Repository.Entities;

namespace Tgs.Agent.Dividend.IntegrationTest
{
    public class OneLayerDistributionTests
    {
        [SetUp]
        public void Setup()
        {
            var fakeTgsContext = Substitute.For<tgsContext>();
            var fakeMemoryCache = Substitute.For<IMemoryCache>();
            var fakeDistribution = Substitute.For<IDistribution>();

            _services.AddKeyedSingleton<IDistribution, OneLayerDistribution>(ContractDividendType.OneLayer.ToString());
            _services.AddSingleton(fakeDistribution)
                .AddSingleton(_fakeRepository)
                .AddSingleton(_fakeContractRepository)
                .AddSingleton(fakeTgsContext)
                .AddSingleton(fakeMemoryCache)
                .AddSingleton(_fakeMediator);
        }

        private readonly ServiceCollection _services = [];
        private readonly IMediator _fakeMediator = Substitute.For<IMediator>();
        private readonly IRepository _fakeRepository = Substitute.For<IRepository>();
        private readonly IContractRepository _fakeContractRepository = Substitute.For<IContractRepository>();

        [Test]
        public async Task Create_Agent_Player_Dividends()
        {
            var sp = _services.BuildServiceProvider();

            var brandId = Guid.NewGuid();
            var agentId = Guid.NewGuid();
            var agentId2 = Guid.NewGuid();

            var from = DateTime.Parse("2023/12/06 00:00:00+8");
            var to = DateTime.Parse("2023/12/06 00:00:00+8");

            _fakeRepository.GetNonArchivedBrandIdsAsyncEnumerable(Arg.Any<Guid?>())
                .Returns(new[] { Guid.NewGuid() }.ToAsyncEnumerable());

            var contracts = new[]
            {
                CreateContractFixture(brandId, agentId, ContractDividendType.OneLayer, 0, 0, 0)
                    .Create<AgentDividendContractModel>(),
                CreateContractFixture(brandId, agentId2, ContractDividendType.OneLayer, 0, 0, 0)
                    .Create<AgentDividendContractModel>(),
            };

            var brand = CreateBrandFixture(brandId, from, to, 0, AgentDividendType.OneLayer).Create<BrandModel>();

            var agents = new[]
            {
                CreateAgentFixture(brandId, agentId2, agentId, 10, new LTree($"{agentId:N}.{agentId2:N}"))
                    .Create<AgentModel>(),
                CreateAgentFixture(brandId, agentId, null, 50, new LTree($"{agentId:N}")).Create<AgentModel>()
            };

            var agentPath = new[]
            {
                new AgentDividendPercentageData
                {
                    AgentId = agentId,
                    ParentAgentId = null,
                    DividendPercentage = 50
                }
            };

            var agent2Path = agentPath.Append(new AgentDividendPercentageData
            {
                AgentId = agentId2,
                ParentAgentId = agentId,
                DividendPercentage = 10
            });

            var agentPlayerAggregates = new[]
            {
                CreateAgentPlayerAggregateFixture(brandId, agentId2, from, to, 100, 10, JsonSerializer.Serialize(agent2Path))
                    .Create<AgentPlayerAggregateModel>()
            };

            _fakeContractRepository.GetAgentDividendContractsAsync(Arg.Any<Guid>())
                .Returns(contracts.ToAsyncEnumerable());

            _fakeRepository.GetBrandAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(brand);
            _fakeRepository.GetMaxAgentDividendIdAsync(Arg.Any<CancellationToken>()).Returns(99);
            _fakeRepository.GetAgentsCacheAsyncEnumerable(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                .Returns(agents.ToAsyncEnumerable());

            _fakeRepository
                .GetAgentPlayerAggregatesCacheAsync(brandId, from, to, Arg.Any<CancellationToken>())
                .Returns(agentPlayerAggregates.ToAsyncEnumerable());

            _fakeMediator.Send(Arg.Any<GetNoSendDividendRequest>()).Returns(new NoSendDividendResponse
            {
                Model = Enumerable.Empty<CreateAgentDividendModel>(),
                NegativeDividendsIds = new Dictionary<int, int> { { 1, 1 } }
            });

            var @event = new AgentDividendRequest(brandId);

            var sut = new AgentDividendHandler(_fakeRepository,
                _fakeContractRepository,
                _fakeMediator,
                sp);

            await sut.Handle(@event, default);

            _ = _fakeRepository.ReceivedWithAnyArgs(1).CreateAgentDividendsAsync(null!, null!, null!, default);
            _ = _fakeMediator.Received(1).Send(Arg.Any<GetNoSendDividendRequest>(), Arg.Any<CancellationToken>());

            _ = _fakeRepository.Received(1)
                .CreateAgentDividendsAsync(Arg.Any<BrandModel>(),
                    Arg.Any<IEnumerable<CreateAgentDividendModel>>(),
                    Arg.Any<IDictionary<int, int>>(),
                    Arg.Any<CancellationToken>());
        }

        private Fixture CreateContractFixture(Guid brandId, Guid agentId, ContractDividendType contractDividendType, decimal validBetAmount, decimal depositAmount, int betDays)
        {
            var fixture = new Fixture();
            fixture.Customize<AgentDividendContractModel>(u => u
                .With(s => s.BrandId, brandId)
                .With(s => s.AgentId, agentId)
                .With(s => s.ContractStatus, 0)
                .With(s => s.ContractDividendType, (short)contractDividendType)
                .With(s => s.DepositAmount, depositAmount)
                .With(s => s.BetDays, betDays)
                .With(s => s.ValidBetAmount, validBetAmount));
            fixture.Behaviors.Add(new OmitOnRecursionBehavior());
            return fixture;
        }

        private Fixture CreateBrandFixture(Guid brandId, DateTime from, DateTime to, decimal platformCharge, AgentDividendType agentDividendType)
        {
            var fixture = new Fixture();
            fixture.Customize<BrandModel>(u => u
                .With(s => s.Id, brandId)
                .With(s => s.SettlementIntervalType, (short)SettlementIntervalType.Daily)
                .With(s => s.CurrentSettlementFrom, from)
                .With(s => s.CurrentSettlementTo, to)
                .With(s => s.PlatformCharge, platformCharge)
                .With(s => s.AgentDividendType, (short)agentDividendType));
            fixture.Behaviors.Add(new OmitOnRecursionBehavior());
            return fixture;
        }

        private Fixture CreateAgentFixture(Guid brandId, Guid agentId, Guid? parentId, decimal dividendPercentage, LTree path)
        {
            var fixture = new Fixture();
            fixture.Customize<AgentModel>(u => u
                .With(s => s.Id, agentId)
                .With(s => s.ParentAgentId, parentId)
                .With(s => s.DividendPercentage, dividendPercentage)
                .With(s => s.Path, path)
                .With(s => s.BrandId, brandId));
            fixture.Behaviors.Add(new OmitOnRecursionBehavior());
            return fixture;
        }

        private static Fixture CreateAgentPlayerAggregateFixture(Guid brandId,
            Guid agentId,
            DateTime from,
            DateTime to,
            decimal winLoss,
            decimal agentDividendPercentage,
            string path)
        {
            var fixture = new Fixture();
            fixture.Customize<AgentPlayerAggregateModel>(u => u
                .With(s => s.BrandId, brandId)
                .With(s => s.AgentId, agentId)
                .With(s => s.From, from)
                .With(s => s.To, to)
                .With(s => s.WinLossAmount, winLoss)
                .With(s => s.BonusAmount, 0)
                .With(s => s.BonusAdjustAmount, 0)
                .With(s => s.NoIncludeNetProfitBonusAdjustAmount, 0)
                .With(s => s.AgentDividendPercentage, agentDividendPercentage)
                .With(s => s.AgentDividendPercentageTreeData, path));
            fixture.Behaviors.Add(new OmitOnRecursionBehavior());
            return fixture;
        }

    }
}