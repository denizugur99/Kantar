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
        
        [HttpGet("GetAll/{pagesize?}/{page?}")]
        public async Task<IActionResult> GetAll(int pagesize ,int page)
        {
            var response = await _mediator.Send(new ProductsQuery()
            {
                PageCount=page,
                PageSize=pagesize,
                
            });
            return CreateActionResultInstance(response);
        }
        [HttpPost("send")]
        public async Task<IActionResult> Send([FromBody] ShipProductCommand command)
        {
            var response = await _mediator.Send(command);
            return CreateActionResultInstance(response);


        }
        [HttpGet("GetWithTime/{pagesize?}/{pagenumber?}/{first_time}/{last_time}")]
        public async Task<IActionResult> GetWithTime(int pagesize,int pagenumber,DateTime first_time,DateTime last_time, [FromQuery]string?search)
        {
            var response = await _mediator.Send(new GetProductsWithTimeQuery()
            {
                pageNumber=pagenumber,
                pageSize=pagesize,
                FirstDate = first_time,
                LastDate = last_time,
                search=search
            });
            return CreateActionResultInstance(response);
        }
    }
}
