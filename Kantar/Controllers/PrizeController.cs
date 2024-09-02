using Kantar.Command;
using Kantar.Queries;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Kantar.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PrizeController : CustomControllerBase
    {
        private readonly IMediator _mediator;

        public PrizeController(IMediator mediator)
        {
            _mediator = mediator;
        }
        [HttpPost("UnitPrice")]
        public async Task<IActionResult> UnitPrice([FromBody] AddUnitPriceCommand command)
        {
            var response = await _mediator.Send(command);
            return CreateActionResultInstance(response);
        }
        [HttpGet("getlist")]
        public async Task<IActionResult> GetPriceList()
        {
            var result = await _mediator.Send(new PriceQuery());
            return CreateActionResultInstance(result);
        }
        [HttpPut("updateprize")]
        public async Task<IActionResult> UpdatePrize(UpdatePrizeCommand command) {
            var result=await _mediator.Send(command);
            return CreateActionResultInstance(result);
        }

            

    }
}
