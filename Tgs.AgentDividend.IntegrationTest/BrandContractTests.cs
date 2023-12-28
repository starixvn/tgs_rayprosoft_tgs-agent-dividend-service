using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Tgs.Agent.Dividend.Client;
using Tgs.Agent.Dividend.Client.Enums;
using Tgs.Agent.Dividend.Client.Models;
using Tgs.Agent.Dividend.Domain.Enums;
using Tgs.Agent.Dividend.Domain.Interfaces.Repositories;
using Tgs.Agent.Dividend.Domain.Models.AgentContract;
using Tgs.Agent.Dividend.Domain.Models.BrandContract;
using Tgs.Agent.Dividend.Repository.Entities;
using ContractGameType = Tgs.Agent.Dividend.Client.Enums.ContractGameType;

namespace Tgs.Agent.Dividend.IntegrationTest;

public class BrandContractTests
{
    [Test]
    public async Task Create_Brand_Contract()
    {
        var fakeDbContext = Substitute.For<tgsContext>();
        var fakeContractRepository = Substitute.For<IContractRepository>();

        var brandId = Guid.NewGuid();

        await using var web = new TestAgentDividendApplication(builder => builder.ConfigureServices(services =>
        {
            services.AddSingleton(fakeDbContext)
                .AddSingleton(fakeContractRepository);
        }));

        var httpClient = web.CreateClient();

        var sut = new BrandContractClient(httpClient);

        await sut.CreateBrandContractAsync(new CreateBrandContract(brandId,"name",0,1,1,1,1,"editor",new[]
            {
                ContractGameType.CardGame
            }, []), default);

        _ = fakeContractRepository.ReceivedWithAnyArgs(1).CreateBrandContractAsync(null!, default);
    }

    [Test]
    public async Task Get_Brand_Contract()
    {
        var fakeDbContext = Substitute.For<tgsContext>();
        var fakeContractRepository = Substitute.For<IContractRepository>();

        var brandId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var agentContractId = Guid.NewGuid();

        fakeContractRepository.GetBrandContractsAsync(Arg.Any<Guid>())
            .Returns(new BrandContractModel[]
            {
                new BrandContractModel(agentContractId,
                    brandId,
                    "name",
                    (short)ContractDividendType.OnePercentage,
                    1,
                    1,
                    1,
                    1,
                    [1, 2],
                    new BrandContractConditionModel[]
                    {
                        new BrandContractConditionModel(1, 1, 1, 1, 1)
                    })
            }.ToAsyncEnumerable());

        await using var web = new TestAgentDividendApplication(builder => builder.ConfigureServices(services =>
        {
            services.AddSingleton(fakeDbContext)
                .AddSingleton(fakeContractRepository);
        }));

        var httpClient = web.CreateClient();

        var sut = new BrandContractClient(httpClient);

        var response = await sut.GetBrandContractAsync(agentId, default).ToArrayAsync();

        Assert.That(response.Length, Is.GreaterThan(0));
    }
}