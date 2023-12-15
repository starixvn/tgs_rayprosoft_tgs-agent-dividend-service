using System;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using AutoFixture;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NUnit.Framework;
using NUnit.Framework.Internal.Execution;
using Tgs.Agent.Dividend.Client;
using Tgs.Agent.Dividend.Core;
using Tgs.Agent.Dividend.Core.Workers;
using Tgs.Agent.Dividend.Domain.Enums;
using Tgs.Agent.Dividend.Domain.Events;
using Tgs.Agent.Dividend.Domain.Interfaces;
using Tgs.Agent.Dividend.Domain.Interfaces.Repositories;
using Tgs.Agent.Dividend.Domain.Models;
using Tgs.Agent.Dividend.Repository.Entities;

namespace Tgs.Agent.Dividend.IntegrationTest
{
    public class GenerateAgentDividendTests
    {
        [Test]
        public async Task Create_Agent_Player_Dividends()
        {
            var fakeDbContext = Substitute.For<tgsContext>();
            var fakeRepository = Substitute.For<IRepository>();
            
            var brandId = Guid.NewGuid();

            fakeRepository.GetNonArchivedBrandIdsAsyncEnumerable(Arg.Any<Guid?>())
                .Returns(new[] { brandId }.ToAsyncEnumerable());

            await using var web = new TestAgentDividendApplication(builder => builder.ConfigureServices(services =>
            {
                services.AddSingleton(fakeDbContext)
                    .AddSingleton(fakeRepository);
            }));

            var httpClient = web.CreateClient();

            var sut = new AgentDividendClient(httpClient);

            await sut.GenerateAgentDividend();

            _ = fakeRepository.ReceivedWithAnyArgs(1).GetNonArchivedBrandIdsAsyncEnumerable(null!);
        }
    }
}