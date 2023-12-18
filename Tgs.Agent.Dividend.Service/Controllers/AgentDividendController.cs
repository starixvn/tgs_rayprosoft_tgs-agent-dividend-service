using MediatR;
using Microsoft.AspNetCore.Mvc;
using Tgs.Agent.Dividend.Domain.Models;

namespace Tgs.Agent.Dividend.Service.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AgentDividendController : ControllerBase
{
    [HttpPost]
    public Task AgentDividendGenerationAsync([FromServices] IMediator mediator)
    {
        return mediator.Send(new GenerateAgentDividendRequest());
    }

    [HttpPost("{brandId:guid}")]
    public Task AgentDividendGenerationAsync([FromServices] IMediator mediator, Guid brandId)
    {
        return mediator.Send(new GenerateAgentDividendRequest(brandId));
    }
}