using AutoFixture;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Tgs.Agent.Dividend.Core.AgentDividend;
using Tgs.Agent.Dividend.Core.Distributions;
using Tgs.Agent.Dividend.Core.Workers;
using Tgs.Agent.Dividend.Domain.Enums;
using Tgs.Agent.Dividend.Domain.Interfaces;
using Tgs.Agent.Dividend.Domain.Interfaces.Repositories;
using Tgs.Agent.Dividend.Domain.Models;

namespace Tgs.Agent.Dividend.UnitTest;

public class AgentDividendHandlerTests
{
    [Test]
    public async Task Create_Agent_Player_Dividends()
    {
        var fakeMediator = Substitute.For<IMediator>();
        var fakeRepository = Substitute.For<IRepository>();
        var fakeBackgroundTaskQueue = Substitute.For<IBackgroundTaskQueue>();

        var services = new ServiceCollection();
        services.AddKeyedSingleton<IDividendCalculator, OneLayerDividendCalculator>(ContractDividendType.OneLayer
            .ToString());
        services
            .AddSingleton(fakeRepository)
            .AddSingleton(fakeMediator)
            .AddSingleton(fakeBackgroundTaskQueue);

        var brandId = Guid.NewGuid();

        var from = DateTime.Parse("2023/12/06 00:00:00+8");
        var to = DateTime.Parse("2023/12/06 00:00:00+8");

        var brand = CreateBrandFixture(brandId, from, to, 0, true, AgentDividendType.OneLayer).Create<BrandModel>();

        fakeRepository.GetBrandAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(brand);

        fakeMediator.Send(Arg.Any<GetNoSendDividendRequest>())
            .Returns(new NoSendDividendResponse
            {
                Model = Enumerable.Empty<AgentDividendModel>(),
                NegativeDividendsIds = new Dictionary<int, int> { { 1, 1 } }
            });

        var request = new CreateAgentDividendRequest(brandId,
            Enumerable.Empty<AgentDividendModel>(),
            new Dictionary<int, int> { { 1, 1 } });

        var sut = new CreateAgentDividendHandler(
            fakeRepository,
            fakeBackgroundTaskQueue);

        await sut.Handle(request, default);

        _ = fakeRepository.ReceivedWithAnyArgs(1).CreateAgentDividendsAsync(null!, null!, null!, null!, default);
    }

    private static Fixture CreateBrandFixture(Guid brandId,
        DateTime from,
        DateTime to,
        decimal platformCharge,
        bool checkDividendActiveCount,
        AgentDividendType agentDividendType)
    {
        var fixture = new Fixture();
        fixture.Customize<BrandModel>(u => u
            .With(s => s.Id, brandId)
            .With(s => s.SettlementIntervalType, (short)SettlementIntervalType.Daily)
            .With(s => s.CurrentSettlementFrom, from)
            .With(s => s.CurrentSettlementTo, to)
            .With(s => s.PlatformCharge, platformCharge)
            .With(s => s.CheckDividendActiveCount, checkDividendActiveCount)
            .With(s => s.AgentDividendType, (short)agentDividendType));
        fixture.Behaviors.Add(new OmitOnRecursionBehavior());
        return fixture;
    }
}