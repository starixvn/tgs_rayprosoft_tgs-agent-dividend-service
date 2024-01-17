using MediatR;
using Microsoft.AspNetCore.Mvc;
using Tgs.Agent.Client;
using Tgs.Agent.Client.Models;
using Tgs.Agent.Dividend.Core.Workers;
using Tgs.Agent.Dividend.Domain.Models;
using Tgs.Agent.Dividend.Domain.Models.BrandContract;

namespace Tgs.Agent.Dividend.Service.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AgentDividendController(IMediator mediator, IBackgroundTaskQueue queue) : ControllerBase
{
    [HttpPost]
    public Task AgentDividendGenerationAsync()
    {
        return mediator.Send(new GenerateAgentDividendRequest());
    }

    [HttpPost("{brandId:guid}")]
    public Task AgentDividendGenerationAsync(Guid brandId)
    {
        return mediator.Send(new GenerateAgentDividendRequest(brandId));
    }

    [HttpPost("send-agent-dividend")]
    public async Task ManualSendAgentDividendAsync(Guid[] agentDividendRecordIds, string editorUsername)
    {
        foreach (var agentDividendRecordId in agentDividendRecordIds)
            await queue.QueueBackgroundWorkItemAsync(async (scope, token) =>
            {
                var innerMediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                await innerMediator.Send(new ManualSendAgentDividendRequest(agentDividendRecordId, editorUsername),
                    token);
            });
    }

    [HttpGet("{brandId:guid}/{agentId:guid}/beforehand")]
    public Task<AgentDividendBeforehandResponse> GetAgentDividendBeforehandAsync(
        Guid brandId,
        Guid agentId)
    {
        return mediator.Send(new GetAgentDividendBeforehandRequest(brandId, agentId));
    }

    [HttpGet("records")]
    public IAsyncEnumerable<AgentDividendRecordModel> GetAgentDividendRecordsAsync(
        [FromQuery] GetAgentDividendRecordsRequest request)
    {
        return mediator.CreateStream(new GetAgentDividendRecordsRequest(request.BrandId,
            request.From,
            request.To,
            request.AgentUsername));
    }

    [HttpGet("{brandId:guid}/pre-calculate")]
    public async IAsyncEnumerable<AgentDividendModel> GetPreCalculateAgentDividendAsync(
        [FromServices] IAgentClient agentClient,
        Guid brandId)
    {
        var response = await mediator.Send(new AgentDividendRequest(brandId));

        foreach (var model in response.Model)
        {
            var agent = await agentClient.GetAgentInfoAsync(new GetAgentInfoRequest(model.AgentId));

            model.AgentUsername = agent.Username;

            yield return model;
        }
    }
}