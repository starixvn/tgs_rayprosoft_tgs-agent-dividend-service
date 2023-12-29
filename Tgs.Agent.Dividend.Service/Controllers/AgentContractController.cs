using MediatR;
using Microsoft.AspNetCore.Mvc;
using Tgs.Agent.Dividend.Domain.Models.AgentContract;

namespace Tgs.Agent.Dividend.Service.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AgentContractController(ISender sender) : ControllerBase
{
    [HttpGet("{agentId:guid}")]
    public IAsyncEnumerable<AgentDividendContractViewModel> GetBrandContractAsync(Guid agentId)
    {
        return sender.CreateStream(new GetAgentDividendContractsRequest(agentId));
    }

    [HttpPost]
    public Task CreateAgentContractAsync([FromBody] CreateAgentContractRequest request)
    {
        return sender.Send(request);
    }
}