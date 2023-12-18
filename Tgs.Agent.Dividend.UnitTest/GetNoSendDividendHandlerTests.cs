using AutoFixture;
using NSubstitute;
using Tgs.Agent.Dividend.Core;
using Tgs.Agent.Dividend.Domain.Interfaces.Repositories;
using Tgs.Agent.Dividend.Domain.Models;

namespace Tgs.Agent.Dividend.UnitTest;

public class GetNoSendDividendHandlerTests
{
    [Test]
    public async Task Get_No_Send_Dividends()
    {
        var fakeRepository = Substitute.For<IRepository>();

        var brandId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var agentId2 = Guid.NewGuid();
        var agentId3 = Guid.NewGuid();

        var models = new[]
        {
            CreateCreateAgentDividendModelFixture(1, brandId, agentId, 50, 50).Create<CreateAgentDividendModel>(),
            CreateCreateAgentDividendModelFixture(2, brandId, agentId, 40, 100).Create<CreateAgentDividendModel>(),
            CreateCreateAgentDividendModelFixture(3, brandId, agentId2, 33, 100).Create<CreateAgentDividendModel>(),
            CreateCreateAgentDividendModelFixture(4, brandId, agentId3, 33, -100).Create<CreateAgentDividendModel>()
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
            Assert.That(new[] { 1, 2 }, Does.Contain(response.NegativeDividendsIds[11]));
            Assert.That(new[] { 1, 2 }, Does.Contain(response.NegativeDividendsIds[33]));
            Assert.That(response.NegativeDividendsIds[34], Is.EqualTo(3));
            Assert.That(response.NegativeDividendsIds[35], Is.EqualTo(3));
            Assert.That(response.Model.Where(x => x.AgentId == agentId).Sum(s => s.AgentDividendAmount),
                Is.EqualTo(58));
            Assert.That(response.Model.Where(x => x.AgentId == agentId2).Sum(s => s.AgentDividendAmount),
                Is.EqualTo(34));
        });
    }

    private Fixture CreateCreateAgentDividendModelFixture(int id,
        Guid brandId,
        Guid agentId,
        decimal agentDividendPercentage,
        decimal agentDividendAmount)
    {
        var fixture = new Fixture();
        fixture.Customize<CreateAgentDividendModel>(u => u
            .With(s => s.Id, id)
            .With(s => s.BrandId, brandId)
            .With(s => s.AgentId, agentId)
            .With(s => s.AgentDividendPercentage, agentDividendPercentage)
            .With(s => s.AgentDividendAmount, agentDividendAmount)
            .With(s => s.BeforeDividendAmount, 0));
        fixture.Behaviors.Add(new OmitOnRecursionBehavior());
        return fixture;
    }

    private Fixture CreateNegativeAgentDividendModelFixture(int id, Guid agentId, decimal agentDividendAmount)
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