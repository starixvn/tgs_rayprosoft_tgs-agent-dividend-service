using MediatR;
using Microsoft.AspNetCore.Mvc;
using Tgs.Agent.Dividend.Domain.Models;

namespace Tgs.Agent.Dividend.Service.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BrandContractController(ISender sender) : ControllerBase
    {
        [HttpGet("{brandId:guid}")]
        public IAsyncEnumerable<BrandContract> GetBrandContractAsync(Guid brandId)
        {
            return sender.CreateStream(new GetBrandContractRequest(brandId));
        }

        [HttpPost]
        public Task CreateBrandContractAsync([FromBody] CreateBrandContractRequest request)
        {
            return sender.Send(request);
        }
    }
}
