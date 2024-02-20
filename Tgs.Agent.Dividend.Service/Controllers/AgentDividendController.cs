using MediatR;
using Microsoft.AspNetCore.Mvc;
using Tgs.Agent.Client;
using Tgs.Agent.Client.Models;
using Tgs.Agent.Dividend.Domain.Models;
using Tgs.Agent.Dividend.Domain.Models.BrandContract;

namespace Tgs.Agent.Dividend.Service.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AgentDividendController(IMediator mediator) : ControllerBase
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
    public async Task ManualSendAgentDividendAsync([FromBody] ManualSendAgentDividendsRequest request)
    {
        foreach (var agentDividendRecordId in request.AgentDividendRecordIds)
            await mediator.Send(new ManualSendAgentDividend(agentDividendRecordId,
                request.Approve,
                request.EditorUsername,
                request.Remark));
    }

    [HttpPut("{agentId}/clear-negative-dividend/{clearByUserId}")]
    public Task ClearNegativeDividendAsync(Guid agentId, Guid clearByUserId)
    {
        return mediator.Send(new ClearNegativeDividend(agentId, clearByUserId));
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
            request.Status,
            request.From,
            request.To,
            request.ApprovedFrom,
            request.ApprovedTo,
            request.AgentUsername,
            request.NegativeAgentDividendCarryForward));
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

            model.AgentUsername = agent!.Username;

            yield return model;
        }
    }
}