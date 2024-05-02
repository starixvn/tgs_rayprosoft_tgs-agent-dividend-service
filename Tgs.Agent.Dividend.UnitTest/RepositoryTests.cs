using System.Text.Json;
using AutoFixture;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MockQueryable.Moq;
using Moq;
using NSubstitute;
using Tgs.Agent.Dividend.Domain.Interfaces.Repositories;
using Tgs.Agent.Dividend.Domain.Models;
using Tgs.Agent.Dividend.Repository.Entities;

namespace Tgs.Agent.Dividend.UnitTest;

public class RepositoryTests
{
    [Test]
    public async Task Get_Agent_Player_Aggregates()
    {
        var fakeMemoryCacheKeyMapper = Substitute.For<IMemoryCacheKeyMapper>();
        var fakeMemoryCache = Substitute.For<IMemoryCache>();
        var faketgsContext = Substitute.For<tgsContext>();

        var sut = new Repository.Repository(faketgsContext, fakeMemoryCache, fakeMemoryCacheKeyMapper);

        var brandId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var agentId2 = Guid.NewGuid();
        var from = DateTime.Parse("2023/12/06 00:00:00+8");
        var to = DateTime.Parse("2023/12/06 00:00:00+8");

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

        var agent2Path2 = new[]
        {
            new AgentDividendPercentageData
            {
                AgentId = agentId2,
                ParentAgentId = agentId,
                DividendPercentage = 10
            },
            new AgentDividendPercentageData
            {
                AgentId = agentId,
                ParentAgentId = null,
                DividendPercentage = 50
            }
        };

        var agentPlayerAggregates = new[]
        {
            CreateAgentPlayerAggregateFixture(brandId,
                    agentId2,
                    from,
                    to,
                    5,
                    0,
                    10,
                    JsonSerializer.Serialize(agent2Path))
                .Create<PlayerAggregate>(),
            CreateAgentPlayerAggregateFixture(brandId,
                    agentId2,
                    from,
                    to,
                    5,
                    0,
                    10,
                    JsonSerializer.Serialize(agent2Path2))
                .Create<PlayerAggregate>()
        }.AsQueryable()
        .BuildMock();

        var agentPlayerGameAggregates = new[]
        {
            CreateAgentPlayerGameAggregateFixture(brandId,
                    agentId2,
                    from,
                    to)
                .Create<PlayerGameRebateAggregate>()
        }.AsQueryable()
        .BuildMock();

        var mockPlayerAggregates = GetDbSet(agentPlayerAggregates);
        var mockPlayerGameAggregates = GetDbSet(agentPlayerGameAggregates);

        faketgsContext.PlayerAggregates.Returns(mockPlayerAggregates.Object);
        faketgsContext.PlayerGameRebateAggregates.Returns(mockPlayerGameAggregates.Object);

        var response = await sut.GetAgentPlayerAggregatesCacheAsync(brandId, from, to, default).ToArrayAsync();

        Assert.That(response.Length, Is.EqualTo(1));
        Assert.That(response.First().LiveDealerWinLoss, Is.EqualTo(10));
        Assert.That(response.First().WinLossAmount, Is.EqualTo(10));
    }

    private static Fixture CreateAgentPlayerAggregateFixture(Guid brandId,
        Guid agentId,
        DateTime from,
        DateTime to,
        decimal winLoss,
        decimal agentAdjustAmount,
        decimal agentDividendPercentage,
        string path)
    {
        var fixture = new Fixture();
        fixture.Customize<PlayerAggregate>(u => u
            .With(s => s.BrandId, brandId)
            .With(s => s.AgentId, agentId)
            .With(s => s.ReportDate, from)
            .With(s => s.Hour, 10)
            .With(s => s.WinLossAmount, winLoss)
            .With(s => s.BonusAmount, 0)
            .With(s => s.BonusAdjustAmount, 0)
            .With(s => s.NoIncludeNetProfitBonusAdjustAmount, 0)
            .With(s => s.AgentAdjustAmount, agentAdjustAmount)
            .With(s => s.AgentDividendPercentage, agentDividendPercentage)
            .With(s => s.AgentDividendPercentageTreeData, path));
        fixture.Behaviors.Add(new OmitOnRecursionBehavior());
        return fixture;
    }

    private static Fixture CreateAgentPlayerGameAggregateFixture(Guid brandId, Guid agentId, DateTime from, DateTime to)
    {
        var fixture = new Fixture();
        fixture.Customize<PlayerGameRebateAggregate>(u => u
            .With(s => s.BrandId, brandId)
            .With(s => s.AgentId, agentId)
            .With(s => s.ReportDate, from)
            .With(s => s.GameType, 5)
            .With(s => s.Hour, 10)
            .With(s => s.WinLoss, 10));
        fixture.Behaviors.Add(new OmitOnRecursionBehavior());
        return fixture;
    }

    private Mock<DbSet<T>> GetDbSet<T>(IQueryable<T> data) where T : class
    {
        var mockSet = new Mock<DbSet<T>>();
        mockSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(data.Provider);
        mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(data.Expression);
        mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(data.ElementType);
        mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(data.GetEnumerator());
        return mockSet;
    }
}