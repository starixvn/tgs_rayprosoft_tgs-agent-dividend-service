using AutoFixture;
using NSubstitute;
using Tgs.Agent.Dividend.Core.AgentDividend;
using Tgs.Agent.Dividend.Domain.Interfaces.Repositories;
using Tgs.Agent.Dividend.Domain.Models;

namespace Tgs.Agent.Dividend.UnitTest;

public class GetNoSendDividendHandlerTests
{
    private static readonly int[] Actual = [101, 102];
    private static readonly int[] ActualArray = [101, 102];

    [Test]
    public async Task Get_No_Send_Dividends()
    {
        var fakeRepository = Substitute.For<IRepository>();

        var brandId = Guid.NewGuid();
        var agentId = Guid.Parse("1d6f6c96-c0f6-ec11-8db1-70b5e82b6b7c");
        var agentId2 = Guid.Parse("2d6f6c96-c0f6-ec11-8db1-70b5e82b6b7c");
        var agentId3 = Guid.Parse("3d6f6c96-c0f6-ec11-8db1-70b5e82b6b7c");

        var models = new[]
        {
            CreateCreateAgentDividendModelFixture(101, brandId, agentId, 50, 50).Create<AgentDividendModel>(),
            CreateCreateAgentDividendModelFixture(102, brandId, agentId, 40, 100).Create<AgentDividendModel>(),
            CreateCreateAgentDividendModelFixture(103, brandId, agentId2, 33, 100).Create<AgentDividendModel>(),
            CreateCreateAgentDividendModelFixture(104, brandId, agentId3, 33, -100).Create<AgentDividendModel>()
        };

        var agentNegatives = new[]
        {
            CreateNegativeAgentDividendModelFixture(11, agentId, -99).Create<NegativeAgentDividendModel>(),
            CreateNegativeAgentDividendModelFixture(22, agentId, 40).Create<NegativeAgentDividendModel>(),
            CreateNegativeAgentDividendModelFixture(33, agentId, -33).Create<NegativeAgentDividendModel>()
        };

        var agent2Negatives = new[]
        {
            CreateNegativeAgentDividendModelFixture(34, agentId2, -33).Create<NegativeAgentDividendModel>(),
            CreateNegativeAgentDividendModelFixture(35, agentId2, -33).Create<NegativeAgentDividendModel>()
        };

        fakeRepository.GetNegativeAgentDividendsAsync(agentId).Returns(agentNegatives.ToAsyncEnumerable());
        fakeRepository.GetNegativeAgentDividendsAsync(agentId2).Returns(agent2Negatives.ToAsyncEnumerable());
        fakeRepository.GetNegativeAgentDividendsAsync(agentId3)
            .Returns(AsyncEnumerable.Empty<NegativeAgentDividendModel>());

        var request = new GetNoSendDividendRequest(models);

        var sut = new GetNoSendDividendHandler(fakeRepository);

        var response = await sut.Handle(request, default);

        Assert.Multiple(() =>
        {
            Assert.That(response.NegativeDividendsIds[11], Is.EqualTo(102));
            Assert.That(response.NegativeDividendsIds[22], Is.EqualTo(102));
            Assert.That(response.NegativeDividendsIds[33], Is.EqualTo(102));
            Assert.That(response.NegativeDividendsIds[34], Is.EqualTo(103));
            Assert.That(response.NegativeDividendsIds[35], Is.EqualTo(103));
            Assert.That(response.Model.First(x => x.Id == 101).AutoSend, Is.EqualTo(false));
            Assert.That(response.Model.First(x => x.Id == 102).AutoSend, Is.EqualTo(true));
            Assert.That(response.Model.First(x => x.Id == 104).AutoSend, Is.EqualTo(false));
            Assert.That(response.Model.Where(x => x.AgentId == agentId)
                            .Sum(s => s.BeforeDividendAmount ?? s.AgentDividendAmount) +
                        agentNegatives.Sum(s => s.AgentDividendAmount),
                Is.EqualTo(58));
            Assert.That(response.Model.Where(x => x.AgentId == agentId2)
                            .Sum(s => s.BeforeDividendAmount ?? s.AgentDividendAmount) +
                        agent2Negatives.Sum(s => s.AgentDividendAmount),
                Is.EqualTo(34));
        });
    }

    [Test]
    public async Task Get_No_Send_Dividends_Scenario2()
    {
        var fakeRepository = Substitute.For<IRepository>();

        var brandId = Guid.NewGuid();
        var agentId = Guid.Parse("1d6f6c96-c0f6-ec11-8db1-70b5e82b6b7c");
        var agentId2 = Guid.Parse("2d6f6c96-c0f6-ec11-8db1-70b5e82b6b7c");
        var agentId3 = Guid.Parse("3d6f6c96-c0f6-ec11-8db1-70b5e82b6b7c");

        var models = new[]
        {
            CreateCreateAgentDividendModelFixture(101, brandId, agentId, 50, 50).Create<AgentDividendModel>(),
            CreateCreateAgentDividendModelFixture(102, brandId, agentId, 40, 100).Create<AgentDividendModel>(),
            CreateCreateAgentDividendModelFixture(103, brandId, agentId2, 33, 100).Create<AgentDividendModel>(),
            CreateCreateAgentDividendModelFixture(104, brandId, agentId3, 33, -100).Create<AgentDividendModel>()
        };

        var agentNegatives = new[]
        {
            CreateNegativeAgentDividendModelFixture(11, agentId, -40).Create<NegativeAgentDividendModel>(),
            CreateNegativeAgentDividendModelFixture(22, agentId, -99).Create<NegativeAgentDividendModel>(),
            CreateNegativeAgentDividendModelFixture(33, agentId, -33).Create<NegativeAgentDividendModel>()
        };

        var agent2Negatives = new[]
        {
            CreateNegativeAgentDividendModelFixture(34, agentId2, -33).Create<NegativeAgentDividendModel>(),
            CreateNegativeAgentDividendModelFixture(35, agentId2, -33).Create<NegativeAgentDividendModel>()
        };

        fakeRepository.GetNegativeAgentDividendsAsync(agentId).Returns(agentNegatives.ToAsyncEnumerable());
        fakeRepository.GetNegativeAgentDividendsAsync(agentId2).Returns(agent2Negatives.ToAsyncEnumerable());
        fakeRepository.GetNegativeAgentDividendsAsync(agentId3)
            .Returns(AsyncEnumerable.Empty<NegativeAgentDividendModel>());

        var request = new GetNoSendDividendRequest(models);

        var sut = new GetNoSendDividendHandler(fakeRepository);

        var response = await sut.Handle(request, default);

        Assert.Multiple(() =>
        {
            Assert.That(response.NegativeDividendsIds[34], Is.EqualTo(103));
            Assert.That(response.NegativeDividendsIds[35], Is.EqualTo(103));
            Assert.That(response.Model.First(x => x.Id == 101).AutoSend, Is.EqualTo(false));
            Assert.That(response.Model.First(x => x.Id == 102).AutoSend, Is.EqualTo(false));
            Assert.That(response.Model.First(x => x.Id == 104).AutoSend, Is.EqualTo(false));
            Assert.That(response.Model.Where(x => x.AgentId == agentId)
                            .Sum(s => s.BeforeDividendAmount ?? s.AgentDividendAmount) +
                        agentNegatives.Sum(s => s.AgentDividendAmount),
                Is.EqualTo(-22));
            Assert.That(response.Model.Where(x => x.AgentId == agentId2)
                            .Sum(s => s.BeforeDividendAmount ?? s.AgentDividendAmount) +
                        agent2Negatives.Sum(s => s.AgentDividendAmount),
                Is.EqualTo(34));
        });
    }

    [Test]
    public async Task Get_No_Send_Dividends_Scenario3()
    {
        var fakeRepository = Substitute.For<IRepository>();

        var brandId = Guid.NewGuid();
        var agentId = Guid.Parse("1d6f6c96-c0f6-ec11-8db1-70b5e82b6b7c");
        var agentId2 = Guid.Parse("2d6f6c96-c0f6-ec11-8db1-70b5e82b6b7c");
        var agentId3 = Guid.Parse("3d6f6c96-c0f6-ec11-8db1-70b5e82b6b7c");

        var models = new[]
        {
            CreateCreateAgentDividendModelFixture(101, brandId, agentId, 50, 50).Create<AgentDividendModel>(),
            CreateCreateAgentDividendModelFixture(102, brandId, agentId, 40, -10).Create<AgentDividendModel>(),
            CreateCreateAgentDividendModelFixture(103, brandId, agentId2, 33, 100).Create<AgentDividendModel>(),
            CreateCreateAgentDividendModelFixture(104, brandId, agentId3, 33, -100, false).Create<AgentDividendModel>()
        };

        var agentNegatives = new[]
        {
            CreateNegativeAgentDividendModelFixture(11, agentId, -40).Create<NegativeAgentDividendModel>(),
            CreateNegativeAgentDividendModelFixture(22, agentId, 39).Create<NegativeAgentDividendModel>(),
            CreateNegativeAgentDividendModelFixture(33, agentId, -33).Create<NegativeAgentDividendModel>()
        };

        var agent2Negatives = new[]
        {
            CreateNegativeAgentDividendModelFixture(34, agentId2, -33).Create<NegativeAgentDividendModel>(),
            CreateNegativeAgentDividendModelFixture(35, agentId2, -33).Create<NegativeAgentDividendModel>()
        };

        fakeRepository.GetNegativeAgentDividendsAsync(agentId).Returns(agentNegatives.ToAsyncEnumerable());
        fakeRepository.GetNegativeAgentDividendsAsync(agentId2).Returns(agent2Negatives.ToAsyncEnumerable());
        fakeRepository.GetNegativeAgentDividendsAsync(agentId3)
            .Returns(AsyncEnumerable.Empty<NegativeAgentDividendModel>());

        var request = new GetNoSendDividendRequest(models);

        var sut = new GetNoSendDividendHandler(fakeRepository);

        var response = await sut.Handle(request, default);

        Assert.Multiple(() =>
        {
            Assert.That(response.NegativeDividendsIds[11], Is.EqualTo(101));
            Assert.That(response.NegativeDividendsIds[33], Is.EqualTo(101));
            Assert.That(response.NegativeDividendsIds[34], Is.EqualTo(103));
            Assert.That(response.NegativeDividendsIds[35], Is.EqualTo(103));
            Assert.That(response.Model.First(x => x.Id == 101).AutoSend, Is.EqualTo(true));
            Assert.That(response.Model.First(x => x.Id == 102).AutoSend, Is.EqualTo(false));
            Assert.That(response.Model.First(x => x.Id == 104).AutoSend, Is.EqualTo(false));
            Assert.That(response.Model.Where(x => x.AgentId == agentId)
                            .Sum(s => s.BeforeDividendAmount ?? s.AgentDividendAmount) +
                        agentNegatives.Sum(s => s.AgentDividendAmount),
                Is.EqualTo(6));
            Assert.That(response.Model.Where(x => x.AgentId == agentId2)
                            .Sum(s => s.BeforeDividendAmount ?? s.AgentDividendAmount) +
                        agent2Negatives.Sum(s => s.AgentDividendAmount),
                Is.EqualTo(34));
        });
    }

    [Test]
    public async Task Get_No_Send_Dividends_Scenario4()
    {
        var fakeRepository = Substitute.For<IRepository>();

        var brandId = Guid.NewGuid();
        var agentId = Guid.Parse("1d6f6c96-c0f6-ec11-8db1-70b5e82b6b7c");

        var models = new[]
        {
            CreateCreateAgentDividendModelFixture(101, brandId, agentId, 50, 50).Create<AgentDividendModel>()
        };

        var agentNegatives = new[]
        {
            CreateNegativeAgentDividendModelFixture(11, agentId, 40).Create<NegativeAgentDividendModel>()
        };

        fakeRepository.GetNegativeAgentDividendsAsync(agentId).Returns(agentNegatives.ToAsyncEnumerable());

        var request = new GetNoSendDividendRequest(models);

        var sut = new GetNoSendDividendHandler(fakeRepository);

        var response = await sut.Handle(request, default);

        Assert.Multiple(() =>
        {
            Assert.That(response.Model.First(x => x.Id == 101).AutoSend, Is.EqualTo(true));
            Assert.That(
                response.Model.Where(x => x.AgentId == agentId)
                    .Sum(s => s.BeforeDividendAmount ?? s.AgentDividendAmount),
                Is.EqualTo(50));
        });
    }

    [Test]
    public async Task Get_No_Send_Dividends_Scenario5()
    {
        var fakeRepository = Substitute.For<IRepository>();

        var brandId = Guid.NewGuid();
        var agentId = Guid.Parse("1d6f6c96-c0f6-ec11-8db1-70b5e82b6b7c");
        var agentId2 = Guid.Parse("2d6f6c96-c0f6-ec11-8db1-70b5e82b6b7c");
        var agentId3 = Guid.Parse("3d6f6c96-c0f6-ec11-8db1-70b5e82b6b7c");

        var models = new[]
        {
            CreateCreateAgentDividendModelFixture(101, brandId, agentId, 50, -10).Create<AgentDividendModel>(),
            CreateCreateAgentDividendModelFixture(102, brandId, agentId, 40, 50).Create<AgentDividendModel>(),
            CreateCreateAgentDividendModelFixture(103, brandId, agentId2, 33, 100).Create<AgentDividendModel>(),
            CreateCreateAgentDividendModelFixture(104, brandId, agentId3, 33, -100, false).Create<AgentDividendModel>()
        };

        var agentNegatives = new[]
        {
            CreateNegativeAgentDividendModelFixture(11, agentId, -40).Create<NegativeAgentDividendModel>(),
            CreateNegativeAgentDividendModelFixture(22, agentId, 39).Create<NegativeAgentDividendModel>(),
            CreateNegativeAgentDividendModelFixture(33, agentId, -33).Create<NegativeAgentDividendModel>()
        };

        var agent2Negatives = new[]
        {
            CreateNegativeAgentDividendModelFixture(34, agentId2, -33).Create<NegativeAgentDividendModel>(),
            CreateNegativeAgentDividendModelFixture(35, agentId2, -33).Create<NegativeAgentDividendModel>()
        };

        fakeRepository.GetNegativeAgentDividendsAsync(agentId).Returns(agentNegatives.ToAsyncEnumerable());
        fakeRepository.GetNegativeAgentDividendsAsync(agentId2).Returns(agent2Negatives.ToAsyncEnumerable());
        fakeRepository.GetNegativeAgentDividendsAsync(agentId3)
            .Returns(AsyncEnumerable.Empty<NegativeAgentDividendModel>());

        var request = new GetNoSendDividendRequest(models);

        var sut = new GetNoSendDividendHandler(fakeRepository);

        var response = await sut.Handle(request, default);

        Assert.Multiple(() =>
        {
            Assert.That(response.NegativeDividendsIds[11], Is.EqualTo(102));
            Assert.That(response.NegativeDividendsIds[22], Is.EqualTo(102));
            Assert.That(response.NegativeDividendsIds[33], Is.EqualTo(102));
            Assert.That(response.NegativeDividendsIds[34], Is.EqualTo(103));
            Assert.That(response.NegativeDividendsIds[35], Is.EqualTo(103));
            Assert.That(response.Model.First(x => x.Id == 101).ReferencedAgentDividendId, Is.EqualTo(102));
            Assert.That(response.Model.First(x => x.Id == 101).AutoSend, Is.EqualTo(false));
            Assert.That(response.Model.First(x => x.Id == 102).AutoSend, Is.EqualTo(true));
            Assert.That(response.Model.First(x => x.Id == 104).AutoSend, Is.EqualTo(false));
            Assert.That(response.Model.Where(x => x.AgentId == agentId)
                            .Sum(s => s.BeforeDividendAmount ?? s.AgentDividendAmount) +
                        agentNegatives.Sum(s => s.AgentDividendAmount),
                Is.EqualTo(6));
            Assert.That(response.Model.Where(x => x.AgentId == agentId2)
                            .Sum(s => s.BeforeDividendAmount ?? s.AgentDividendAmount) +
                        agent2Negatives.Sum(s => s.AgentDividendAmount),
                Is.EqualTo(34));
        });
    }

    [Test]
    public async Task Get_No_Send_Dividends_Scenario6()
    {
        var fakeRepository = Substitute.For<IRepository>();

        var brandId = Guid.NewGuid();
        var agentId = Guid.Parse("1d6f6c96-c0f6-ec11-8db1-70b5e82b6b7c");
        var agentId2 = Guid.Parse("2d6f6c96-c0f6-ec11-8db1-70b5e82b6b7c");
        var agentId3 = Guid.Parse("3d6f6c96-c0f6-ec11-8db1-70b5e82b6b7c");

        var models = new[]
        {
            CreateCreateAgentDividendModelFixture(101, brandId, agentId, 50, -10).Create<AgentDividendModel>(),
            CreateCreateAgentDividendModelFixture(102, brandId, agentId, 40, -30).Create<AgentDividendModel>(),
            CreateCreateAgentDividendModelFixture(103, brandId, agentId2, 33, 100).Create<AgentDividendModel>(),
            CreateCreateAgentDividendModelFixture(104, brandId, agentId3, 33, -100, false).Create<AgentDividendModel>()
        };

        var agentNegatives = new[]
        {
            CreateNegativeAgentDividendModelFixture(11, agentId, -40).Create<NegativeAgentDividendModel>(),
            CreateNegativeAgentDividendModelFixture(22, agentId, 39).Create<NegativeAgentDividendModel>(),
            CreateNegativeAgentDividendModelFixture(33, agentId, -33).Create<NegativeAgentDividendModel>()
        };

        var agent2Negatives = new[]
        {
            CreateNegativeAgentDividendModelFixture(34, agentId2, -33).Create<NegativeAgentDividendModel>(),
            CreateNegativeAgentDividendModelFixture(35, agentId2, -33).Create<NegativeAgentDividendModel>()
        };

        fakeRepository.GetNegativeAgentDividendsAsync(agentId).Returns(agentNegatives.ToAsyncEnumerable());
        fakeRepository.GetNegativeAgentDividendsAsync(agentId2).Returns(agent2Negatives.ToAsyncEnumerable());
        fakeRepository.GetNegativeAgentDividendsAsync(agentId3)
            .Returns(AsyncEnumerable.Empty<NegativeAgentDividendModel>());

        var request = new GetNoSendDividendRequest(models);

        var sut = new GetNoSendDividendHandler(fakeRepository);

        var response = await sut.Handle(request, default);

        Assert.Multiple(() =>
        {
            Assert.That(response.NegativeDividendsIds, Is.Not.ContainKey(11));
            Assert.That(response.NegativeDividendsIds, Is.Not.ContainKey(22));
            Assert.That(response.NegativeDividendsIds, Is.Not.ContainKey(33));
            Assert.That(response.NegativeDividendsIds[34], Is.EqualTo(103));
            Assert.That(response.NegativeDividendsIds[35], Is.EqualTo(103));
            Assert.That(response.Model.First(x => x.Id == 101).ReferencedAgentDividendId, Is.Not.EqualTo(102));
            Assert.That(response.Model.First(x => x.Id == 101).AutoSend, Is.EqualTo(false));
            Assert.That(response.Model.First(x => x.Id == 102).AutoSend, Is.EqualTo(false));
            Assert.That(response.Model.First(x => x.Id == 104).AutoSend, Is.EqualTo(false));
            Assert.That(response.Model.Where(x => x.AgentId == agentId)
                            .Sum(s => s.BeforeDividendAmount ?? s.AgentDividendAmount) +
                        agentNegatives.Sum(s => s.AgentDividendAmount),
                Is.EqualTo(-74));
            Assert.That(response.Model.Where(x => x.AgentId == agentId2)
                            .Sum(s => s.BeforeDividendAmount ?? s.AgentDividendAmount) +
                        agent2Negatives.Sum(s => s.AgentDividendAmount),
                Is.EqualTo(34));
        });
    }

    [Test]
    public async Task Get_No_Send_Dividends_Scenario7()
    {
        var fakeRepository = Substitute.For<IRepository>();

        var brandId = Guid.NewGuid();
        var agentId = Guid.Parse("1d6f6c96-c0f6-ec11-8db1-70b5e82b6b7c");
        var agentId2 = Guid.Parse("2d6f6c96-c0f6-ec11-8db1-70b5e82b6b7c");
        var agentId3 = Guid.Parse("3d6f6c96-c0f6-ec11-8db1-70b5e82b6b7c");

        var models = new[]
        {
            CreateCreateAgentDividendModelFixture(101, brandId, agentId, 50, -10).Create<AgentDividendModel>(),
            CreateCreateAgentDividendModelFixture(102, brandId, agentId, 40, 50).Create<AgentDividendModel>(),
            CreateCreateAgentDividendModelFixture(103, brandId, agentId2, 33, 100).Create<AgentDividendModel>(),
            CreateCreateAgentDividendModelFixture(104, brandId, agentId3, 33, -100, false).Create<AgentDividendModel>(),
            CreateCreateAgentDividendModelFixture(105, brandId, agentId, 50, 20).Create<AgentDividendModel>(),
            CreateCreateAgentDividendModelFixture(106, brandId, agentId, 40, -10).Create<AgentDividendModel>()
        };

        var agentNegatives = new[]
        {
            CreateNegativeAgentDividendModelFixture(11, agentId, -40).Create<NegativeAgentDividendModel>(),
            CreateNegativeAgentDividendModelFixture(22, agentId, 39).Create<NegativeAgentDividendModel>(),
            CreateNegativeAgentDividendModelFixture(33, agentId, -33).Create<NegativeAgentDividendModel>()
        };

        var agent2Negatives = new[]
        {
            CreateNegativeAgentDividendModelFixture(34, agentId2, -33).Create<NegativeAgentDividendModel>(),
            CreateNegativeAgentDividendModelFixture(35, agentId2, -33).Create<NegativeAgentDividendModel>()
        };

        fakeRepository.GetNegativeAgentDividendsAsync(agentId).Returns(agentNegatives.ToAsyncEnumerable());
        fakeRepository.GetNegativeAgentDividendsAsync(agentId2).Returns(agent2Negatives.ToAsyncEnumerable());
        fakeRepository.GetNegativeAgentDividendsAsync(agentId3)
            .Returns(AsyncEnumerable.Empty<NegativeAgentDividendModel>());

        var request = new GetNoSendDividendRequest(models);

        var sut = new GetNoSendDividendHandler(fakeRepository);

        var response = await sut.Handle(request, default);

        Assert.Multiple(() =>
        {
            Assert.That(response.NegativeDividendsIds[11], Is.EqualTo(102));
            Assert.That(response.NegativeDividendsIds[22], Is.EqualTo(102));
            Assert.That(response.NegativeDividendsIds[33], Is.EqualTo(102));
            Assert.That(response.NegativeDividendsIds[34], Is.EqualTo(103));
            Assert.That(response.NegativeDividendsIds[35], Is.EqualTo(103));
            Assert.That(response.Model.First(x => x.Id == 101).ReferencedAgentDividendId, Is.EqualTo(102));
            Assert.That(response.Model.First(x => x.Id == 101).AutoSend, Is.EqualTo(false));
            Assert.That(response.Model.First(x => x.Id == 102).AutoSend, Is.EqualTo(true));
            Assert.That(response.Model.First(x => x.Id == 104).AutoSend, Is.EqualTo(false));
            Assert.That(response.Model.First(x => x.Id == 105).AutoSend, Is.EqualTo(true));
            Assert.That(response.Model.First(x => x.Id == 106).AutoSend, Is.EqualTo(false));
            Assert.That(response.Model.Where(x => x.AgentId == agentId)
                            .Sum(s => s.BeforeDividendAmount ?? s.AgentDividendAmount) +
                        agentNegatives.Sum(s => s.AgentDividendAmount),
                Is.EqualTo(16));
            Assert.That(response.Model.Where(x => x.AgentId == agentId2)
                            .Sum(s => s.BeforeDividendAmount ?? s.AgentDividendAmount) +
                        agent2Negatives.Sum(s => s.AgentDividendAmount),
                Is.EqualTo(34));
        });
    }

    [Test]
    public async Task Get_No_Send_Dividends_Scenario8()
    {
        var fakeRepository = Substitute.For<IRepository>();

        var brandId = Guid.NewGuid();
        var agentId = Guid.Parse("1d6f6c96-c0f6-ec11-8db1-70b5e82b6b7c");
        var agentId2 = Guid.Parse("2d6f6c96-c0f6-ec11-8db1-70b5e82b6b7c");
        var agentId3 = Guid.Parse("3d6f6c96-c0f6-ec11-8db1-70b5e82b6b7c");

        var models = new[]
        {
            CreateCreateAgentDividendModelFixture(101, brandId, agentId, 50, -10).Create<AgentDividendModel>(),
            CreateCreateAgentDividendModelFixture(102, brandId, agentId, 40, 50).Create<AgentDividendModel>(),
            CreateCreateAgentDividendModelFixture(103, brandId, agentId2, 33, 100).Create<AgentDividendModel>(),
            CreateCreateAgentDividendModelFixture(104, brandId, agentId3, 33, -100, false).Create<AgentDividendModel>(),
            CreateCreateAgentDividendModelFixture(105, brandId, agentId, 50, -10).Create<AgentDividendModel>(),
            CreateCreateAgentDividendModelFixture(106, brandId, agentId, 40, 20).Create<AgentDividendModel>()
        };

        var agentNegatives = new[]
        {
            CreateNegativeAgentDividendModelFixture(11, agentId, -40).Create<NegativeAgentDividendModel>(),
            CreateNegativeAgentDividendModelFixture(22, agentId, 39).Create<NegativeAgentDividendModel>(),
            CreateNegativeAgentDividendModelFixture(33, agentId, -33).Create<NegativeAgentDividendModel>()
        };

        var agent2Negatives = new[]
        {
            CreateNegativeAgentDividendModelFixture(34, agentId2, -33).Create<NegativeAgentDividendModel>(),
            CreateNegativeAgentDividendModelFixture(35, agentId2, -33).Create<NegativeAgentDividendModel>()
        };

        fakeRepository.GetNegativeAgentDividendsAsync(agentId).Returns(agentNegatives.ToAsyncEnumerable());
        fakeRepository.GetNegativeAgentDividendsAsync(agentId2).Returns(agent2Negatives.ToAsyncEnumerable());
        fakeRepository.GetNegativeAgentDividendsAsync(agentId3)
            .Returns(AsyncEnumerable.Empty<NegativeAgentDividendModel>());

        var request = new GetNoSendDividendRequest(models);

        var sut = new GetNoSendDividendHandler(fakeRepository);

        var response = await sut.Handle(request, default);

        Assert.Multiple(() =>
        {
            Assert.That(response.NegativeDividendsIds[11], Is.EqualTo(102));
            Assert.That(response.NegativeDividendsIds[22], Is.EqualTo(102));
            Assert.That(response.NegativeDividendsIds[33], Is.EqualTo(102));
            Assert.That(response.NegativeDividendsIds[34], Is.EqualTo(103));
            Assert.That(response.NegativeDividendsIds[35], Is.EqualTo(103));
            Assert.That(response.Model.First(x => x.Id == 101).ReferencedAgentDividendId, Is.EqualTo(102));
            Assert.That(response.Model.First(x => x.Id == 105).ReferencedAgentDividendId, Is.EqualTo(106));
            Assert.That(response.Model.First(x => x.Id == 101).AutoSend, Is.EqualTo(false));
            Assert.That(response.Model.First(x => x.Id == 102).AutoSend, Is.EqualTo(true));
            Assert.That(response.Model.First(x => x.Id == 104).AutoSend, Is.EqualTo(false));
            Assert.That(response.Model.First(x => x.Id == 105).AutoSend, Is.EqualTo(false));
            Assert.That(response.Model.First(x => x.Id == 106).AutoSend, Is.EqualTo(true));
            Assert.That(response.Model.Where(x => x.AgentId == agentId)
                            .Sum(s => s.BeforeDividendAmount ?? s.AgentDividendAmount) +
                        agentNegatives.Sum(s => s.AgentDividendAmount),
                Is.EqualTo(16));
            Assert.That(response.Model.Where(x => x.AgentId == agentId2)
                            .Sum(s => s.BeforeDividendAmount ?? s.AgentDividendAmount) +
                        agent2Negatives.Sum(s => s.AgentDividendAmount),
                Is.EqualTo(34));
        });
    }

    private static Fixture CreateCreateAgentDividendModelFixture(int id,
        Guid brandId,
        Guid agentId,
        decimal agentDividendPercentage,
        decimal agentDividendAmount,
        bool negativeAgentDividendCarryForward = true)
    {
        var fixture = new Fixture();
        fixture.Customize<AgentDividendModel>(u => u
            .With(s => s.Id, id)
            .With(s => s.BrandId, brandId)
            .With(s => s.AgentId, agentId)
            .With(s => s.AgentDividendPercentage, agentDividendPercentage)
            .With(s => s.AgentDividendAmount, agentDividendAmount)
            .With(s => s.BeforeDividendAmount, (decimal?)null)
            .With(s => s.NegativeAgentDividendCarryForward, negativeAgentDividendCarryForward)
            .With(s => s.AutoSend, true));
        fixture.Behaviors.Add(new OmitOnRecursionBehavior());
        return fixture;
    }

    private static Fixture CreateNegativeAgentDividendModelFixture(int id, Guid agentId, decimal agentDividendAmount)
    {
        var fixture = new Fixture();
        fixture.Customize<NegativeAgentDividendModel>(u => u
            .With(s => s.Id, id)
            .With(s => s.AgentId, agentId)
            .With(s => s.AgentDividendAmount, agentDividendAmount));
        fixture.Behaviors.Add(new OmitOnRecursionBehavior());
        return fixture;
    }
}