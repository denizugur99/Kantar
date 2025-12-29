using Kantarv2.Command.UnitPrice;
using Kantarv2.Queries.UnitPrice;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Kantarv2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UnitPriceController : BaseController
    {
        private readonly IMediator _mediator;

        public UnitPriceController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [Authorize(Roles = "SuperAdmin,Admin")]
        [HttpPost("add")]
        public async Task<IActionResult> AddUnitPrice([FromBody] AddUnitPrice command)
        {
            var result = await _mediator.Send(command);
            return CreateActionResultInstance(result);
        }
        [Authorize(Roles = "SuperAdmin,Admin,User")]
        [HttpGet("all/{pagesize?}/{pagenumber?}")]
        public async Task<IActionResult> GetAllUnitPrices(int pagesize,int pagenumber ,[FromQuery] string? search)
        {
            var result = await _mediator.Send(new GetAllUnitPrice()
            {
                PageSize = pagesize,
                PageNumber = pagenumber,
                SearchTerm = search
            });
            return CreateActionResultInstance(result);
        }
        [Authorize(Roles = "SuperAdmin,Admin")]
        [HttpPut("update")]
        public async Task<IActionResult> UpdateUnitPrice([FromBody] UpdateUnitPrice command)
        {
            var result = await _mediator.Send(command);
            return CreateActionResultInstance(result);
        }
        [Authorize(Roles = "SuperAdmin,Admin")]
        [HttpDelete("delete")]
        public async Task<IActionResult> DeleteUnitPrice([FromBody] DeleteUnitPrice command)
        {
            var result = await _mediator.Send(command);
            return CreateActionResultInstance(result);
        }
    }
}
