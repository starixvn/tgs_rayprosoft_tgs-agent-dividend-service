using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
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
        var fakeCashServiceClient = Substitute.For<ICashServiceClient>();
        var fakeRepository = Substitute.For<IRepository>();

        var sut = new SendAgentDividendEventHandler(fakeCashServiceClient,
            fakeRepository,
            NullLogger<SendAgentDividendEventHandler>.Instance);

        var brandId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var walletId = Guid.NewGuid();
        const decimal amount = 100M;
        const int agentDividendId = 11;

        var @event = new SendAgentDividendEvent(brandId, agentId, walletId, amount, agentDividendId);

        _ = sut.Handle(@event, default);

        await fakeRepository.UpdateSentOnAsync(Arg.Is(agentDividendId), Arg.Any<CancellationToken>());

        await fakeCashServiceClient.ReceivedWithAnyArgs(1).AgentDividendAsync(default, 0, null, null);
    }
}