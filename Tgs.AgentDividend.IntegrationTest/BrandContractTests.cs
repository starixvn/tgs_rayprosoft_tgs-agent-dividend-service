//using Microsoft.Extensions.DependencyInjection;
//using NSubstitute;
//using Tgs.Agent.Dividend.Client;
//using Tgs.Agent.Dividend.Client.Models;
//using Tgs.Agent.Dividend.Domain.Enums;
//using Tgs.Agent.Dividend.Domain.Interfaces.Repositories;
//using Tgs.Agent.Dividend.Domain.Models.BrandContract;
//using Tgs.Agent.Dividend.Repository.Entities;
//using BrandContractConditionForMultipleLayerModel =
//    Tgs.Agent.Dividend.Client.Models.BrandContractConditionForMultipleLayerModel;
//using ContractGameType = Tgs.Agent.Dividend.Client.Enums.ContractGameType;

//namespace Tgs.Agent.Dividend.IntegrationTest;

//public class BrandContractTests
//{
//    [Test]
//    public async Task Create_Brand_Contract()
//    {
//        var fakeDbContext = Substitute.For<tgsContext>();
//        var fakeContractRepository = Substitute.For<IContractRepository>();

//        var brandId = Guid.NewGuid();

//        await using var web = new TestAgentDividendApplication(builder => builder.ConfigureServices(services =>
//        {
//            services.AddSingleton(fakeDbContext)
//                .AddSingleton(fakeContractRepository);
//        }));

//        var httpClient = web.CreateClient();

//        var sut = new BrandContractClient(httpClient);

//        await sut.CreateBrandContractAsync(new CreateBrandContract(brandId,
//                "name",
//                0,
//                1,
//                1,
//                1,
//                1,
//                "editor",
//                new[]
//                {
//                    ContractGameType.CardGame
//                },
//                []),
//            default);

//        _ = fakeContractRepository.ReceivedWithAnyArgs(1).CreateBrandContractAsync(null!, default);
//    }

//    [Test]
//    public async Task Get_Brand_Contract()
//    {
//        var fakeDbContext = Substitute.For<tgsContext>();
//        var fakeContractRepository = Substitute.For<IContractRepository>();

//        var brandId = Guid.NewGuid();
//        var agentId = Guid.NewGuid();
//        var agentContractId = Guid.NewGuid();

//        fakeContractRepository.GetBrandContractsAsync(Arg.Any<Guid>())
//            .Returns(new BrandContractModel[]
//            {
//                new(agentContractId,
//                    brandId,
//                    "name",
//                    (short)ContractDividendType.OnePercentage,
//                    1,
//                    1,
//                    1,
//                    1,
//                    [1, 2],
//                    new BrandContractConditionModel[]
//                    {
//                        new(1, 1, 1, 1, 1)
//                    })
//            }.ToAsyncEnumerable());

//        await using var web = new TestAgentDividendApplication(builder => builder.ConfigureServices(services =>
//        {
//            services.AddSingleton(fakeDbContext)
//                .AddSingleton(fakeContractRepository);
//        }));

//        var httpClient = web.CreateClient();

//        var sut = new BrandContractClient(httpClient);

//        var response = await sut.GetBrandContractAsync(agentId, default).ToArrayAsync();

//        Assert.That(response.Length, Is.GreaterThan(0));
//    }

//    [Test]
//    public async Task Update_Brand_Contract_Conditions_For_Multiple_Layer()
//    {
//        var fakeDbContext = Substitute.For<tgsContext>();
//        var fakeContractRepository = Substitute.For<IContractRepository>();

//        var brandId = Guid.NewGuid();

//        await using var web = new TestAgentDividendApplication(builder => builder.ConfigureServices(services =>
//        {
//            services.AddSingleton(fakeDbContext)
//                .AddSingleton(fakeContractRepository);
//        }));

//        var httpClient = web.CreateClient();

//        var sut = new BrandContractClient(httpClient);

//        await sut.UpdateBrandContractConditionsForMultipleLayerAsync(brandId,
//            new[]
//            {
//                new BrandContractConditionForMultipleLayerModel(1, 1, 1, 1, 1),
//                new BrandContractConditionForMultipleLayerModel(2, 2, 2, 2, 2)
//            },
//            default);

//        _ = fakeContractRepository.ReceivedWithAnyArgs(1)
//            .UpdateBrandContractConditionsForMultipleLayerAsync(null!, default);
//    }
//}