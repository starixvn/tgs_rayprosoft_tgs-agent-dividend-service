using MediatR;
using Microsoft.AspNetCore.Mvc;
using Tgs.Agent.Dividend.Domain.Models;
using Tgs.Agent.Dividend.Domain.Models.BrandContract;

namespace Tgs.Agent.Dividend.Service.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BrandContractController(ISender sender) : ControllerBase
    {
        [HttpGet("{brandId:guid}")]
        public IAsyncEnumerable<BrandContractModel> GetBrandContractAsync(Guid brandId)
        {
            return sender.CreateStream(new GetBrandContractsRequest(brandId));
        }

        [HttpPost]
        public Task CreateBrandContractAsync([FromBody] CreateBrandContractRequest request)
        {
            return sender.Send(request);
        }
    }
}
