using MediatR;
using Microsoft.AspNetCore.Mvc;
using Tgs.Agent.Dividend.Domain.Models;

namespace Tgs.Agent.Dividend.Service.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AgentDividendController : ControllerBase
{
    [HttpPost]
    public async Task AgentDividendGenerationAsync([FromServices] IMediator mediator)
    {
        await mediator.Send(new GenerateAgentDividendRequest());
    }
}