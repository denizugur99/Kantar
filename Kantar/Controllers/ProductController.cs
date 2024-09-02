using Kantar.Command;
using Kantar.Entities;
using Kantar.Queries;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Kantar.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController : CustomControllerBase
    {
        private readonly IMediator _mediator;
        

        public ProductController(IMediator mediator)
        {
            _mediator = mediator;
        }
        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody]AddProductCommand command)
        {
            var response = await _mediator.Send(command);
            return CreateActionResultInstance(response);
         
         
        }
        
        [HttpGet("GetAll")]
        public async Task<IActionResult> GetAll()
        {
            var response = await _mediator.Send(new ProductsQuery());
            return CreateActionResultInstance(response);
        }
        [HttpPost("send")]
        public async Task<IActionResult> Send([FromBody] ShipProductCommand command)
        {
            var response = await _mediator.Send(command);
            return CreateActionResultInstance(response);


        }
        [HttpGet("GetWithTime/{first_time}/{last_time}")]
        public async Task<IActionResult> GetWithTime(DateTime first_time,DateTime last_time)
        {
            var response = await _mediator.Send(new GetProductsWithTimeQuery()
            {
                FirstDate = first_time,
                LastDate = last_time
            });
            return CreateActionResultInstance(response);
        }
    }
}
