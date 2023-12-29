using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Tgs.Agent.Dividend.Client;
using Tgs.Agent.Dividend.Domain.Enums;
using Tgs.Agent.Dividend.Domain.Interfaces.Repositories;
using Tgs.Agent.Dividend.Domain.Models.AgentContract;
using Tgs.Agent.Dividend.Repository.Entities;
using CreateAgentContractRequest = Tgs.Agent.Dividend.Client.Models.CreateAgentContractRequest;

namespace Tgs.Agent.Dividend.IntegrationTest;

public class AgentContractTests
{
    [Test]
    public async Task Create_Agent_Contract()
    {
        var fakeDbContext = Substitute.For<tgsContext>();
        var fakeContractRepository = Substitute.For<IContractRepository>();

        var brandId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var brandContractId = Guid.NewGuid();

        await using var web = new TestAgentDividendApplication(builder => builder.ConfigureServices(services =>
        {
            services.AddSingleton(fakeDbContext)
                .AddSingleton(fakeContractRepository);
        }));

        var httpClient = web.CreateClient();

        var sut = new AgentContractClient(httpClient);

        await sut.CreateAgentContractAsync(new CreateAgentContractRequest(brandId, agentId, brandContractId),
            default);

        _ = fakeContractRepository.ReceivedWithAnyArgs(1).CreateAgentContractAsync(null!, default);
    }

    [Test]
    public async Task Get_Agent_Contract()
    {
        var fakeDbContext = Substitute.For<tgsContext>();
        var fakeContractRepository = Substitute.For<IContractRepository>();

        var brandId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var agentContractId = Guid.NewGuid();

        fakeContractRepository.GetAgentDividendContractsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new AgentDividendContractViewModel[]
            {
                new()
                {
                    Id = agentContractId,
                    BrandId = brandId,
                    AgentId = agentId,
                    Name = "name",
                    ContractDividendType = (short)ContractDividendType.OnePercentage,
                    ContractStatus = (short)ContractStatus.Enabled,
                    ValidBetAmount = 1,
                    DepositAmount = 1,
                    BetDays = 1,
                    Conditions = new AgentDividendContractConditionViewModel[]
                    {
                        new()
                        {
                            AgentDividendContractId = agentContractId,
                            Rank = 1,
                            ValidNumber = 1,
                            NetProfit = 1,
                            WinLossAmount = 1,
                            Percentage = 1
                        }
                    }
                }
            }.ToAsyncEnumerable());

        await using var web = new TestAgentDividendApplication(builder => builder.ConfigureServices(services =>
        {
            services.AddSingleton(fakeDbContext)
                .AddSingleton(fakeContractRepository);
        }));

        var httpClient = web.CreateClient();

        var sut = new AgentContractClient(httpClient);

        var response = await sut.GetAgentContractAsync(agentId, default).ToArrayAsync();

        Assert.That(response.Length, Is.GreaterThan(0));
    }
}