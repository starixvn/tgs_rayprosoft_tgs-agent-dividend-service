using System;
using AutoFixture;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Tgs.Agent.Dividend.Core;
using Tgs.Agent.Dividend.Core.Workers;
using Tgs.Agent.Dividend.Domain.Enums;
using Tgs.Agent.Dividend.Domain.Interfaces;
using Tgs.Agent.Dividend.Domain.Interfaces.Repositories;
using Tgs.Agent.Dividend.Domain.Models;
using Tgs.Agent.Dividend.Repository.Entities;

namespace Tgs.Agent.Dividend.IntegrationTest
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public async Task Test1()
        {
            var fakeAgentDividendTaskQueue = Substitute.For<IAgentDividendTaskQueue>();
            var fakeRepository = Substitute.For<IRepository>();
            var fakeDbContext = Substitute.For<tgsContext>();
            var fakeContractRepository = Substitute.For<IContractRepository>();
            var fakeMediator = Substitute.For<IMediator>();
            var fakeDistribution = Substitute.For<IDistribution>();
            var fakeServiceProvider = Substitute.For<IServiceProvider>();

            fakeRepository.GetNonArchivedBrandIdsAsyncEnumerable(Arg.Any<Guid?>())
                .Returns(new[] { Guid.NewGuid() }.ToAsyncEnumerable());

            var web = new TestAgentDividendApplication(builder => builder
                .ConfigureServices(services =>
                {
                    services.AddHostedService<AgentDividendBackground>()
                        .AddSingleton(fakeAgentDividendTaskQueue)
                        .AddSingleton(fakeDbContext);

                    services.AddKeyedSingleton(ContractDividendType.OneLayer.ToString(), fakeDistribution);

                }));

            var brandId = Guid.NewGuid();
            var agentId = Guid.NewGuid();
            var agentId2 = Guid.NewGuid();
            var walletId = Guid.NewGuid();
            var from = DateTime.Parse("2023/12/06 00:00:00+8");
            var to = DateTime.Parse("2023/12/06 00:00:00+8");
            const decimal amount = 100M;
            const int agentDividendId = 11;

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
                CreateAgentFixture(brandId, agentId, agentId2, new LTree($"{agentId2:N}.{agentId:N}"))
                    .Create<AgentModel>(),
                CreateAgentFixture(brandId, agentId2, null, new LTree($"{agentId2:N}")).Create<AgentModel>()
            };

            fakeContractRepository.GetAgentDividendContractsAsync(Arg.Any<Guid>())
                .Returns(contracts.ToAsyncEnumerable());

            fakeRepository.GetBrandAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(brand);
            fakeRepository.GetMaxAgentDividendIdAsync(Arg.Any<CancellationToken>()).Returns(99);
            fakeRepository.GetAgentsCacheAsyncEnumerable(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                .Returns(agents.ToAsyncEnumerable());

            var @event = new AgentDividendRequest(brandId);

            var sut = new AgentDividendHandler(fakeRepository,
                fakeContractRepository,
                fakeMediator,
                fakeServiceProvider);

            await sut.Handle(@event, default);

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
                .With(s => s.CurrentSettlementFrom, from)
                .With(s => s.CurrentSettlementTo, to)
                .With(s => s.PlatformCharge, platformCharge)
                .With(s => s.AgentDividendType, (short)agentDividendType));
            fixture.Behaviors.Add(new OmitOnRecursionBehavior());
            return fixture;
        }

        private Fixture CreateAgentFixture(Guid brandId, Guid agentId, Guid? parentId, LTree path)
        {
            var fixture = new Fixture();
            fixture.Customize<AgentModel>(u => u
                .With(s => s.Id, agentId)
                .With(s => s.ParentAgentId, parentId)
                .With(s => s.Path, path)
                .With(s => s.BrandId, brandId));
            fixture.Behaviors.Add(new OmitOnRecursionBehavior());
            return fixture;
        }

    }
}