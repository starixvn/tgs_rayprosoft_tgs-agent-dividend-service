using AutoFixture;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Tgs.Agent.Client;
using Tgs.Agent.Client.Models;
using Tgs.Agent.Dividend.Core.Events;
using Tgs.Agent.Dividend.Domain.Events;
using Tgs.Agent.Dividend.Domain.Interfaces.Repositories;
using Tgs.CashService.Client;

namespace Tgs.Agent.Dividend.UnitTest;

public class SendAgentDividendEventHandlerTests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public async Task Send_Agent_Dividend()
    {
        var fakeAgentClient = Substitute.For<IAgentClient>();
        var fakeCashServiceClient = Substitute.For<ICashServiceClient>();
        var fakeRepository = Substitute.For<IRepository>();

        var sut = new SendAgentDividendEventHandler(
            fakeAgentClient,
            fakeCashServiceClient,
            fakeRepository,
            NullLogger<SendAgentDividendEventHandler>.Instance);

        var brandId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        const decimal amount = 100M;
        const int agentDividendId = 11;

        var fixture = new Fixture();

        fakeAgentClient.GetAgentInfoAsync(Arg.Any<GetAgentInfoRequest>(), Arg.Any<CancellationToken>())
            .Returns(fixture.Create<AgentInfoModel>());

        var @event = new SendAgentDividendEvent(brandId, agentId, amount, agentDividendId, "username");

        _ = sut.Handle(@event, default);

        await fakeRepository.UpdateSentOnAsync(Arg.Is(agentDividendId),
            Arg.Is("username"),
            Arg.Any<CancellationToken>());

        await fakeCashServiceClient.ReceivedWithAnyArgs(1).AgentDividendAsync(default, 0, null, null);
    }
}