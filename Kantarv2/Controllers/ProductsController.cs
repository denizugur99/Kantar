using Kantarv2.Command.Product;
using Kantarv2.Queries.Products;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kantarv2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : BaseController
    {
        private readonly IMediator _mediator;
        public ProductsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [Authorize(Roles = "SuperAdmin,Admin")]
        [HttpPost("add")]
        public async Task<IActionResult> AddProduct(AddProduct command)
        {
            var result = await _mediator.Send(command);
            return CreateActionResultInstance(result);
        }

        [Authorize(Roles = "SuperAdmin,Admin")]
        [HttpDelete("delete")]
        public async Task<IActionResult> DeleteProduct(DeleteProduct command)
        {
            var result = await _mediator.Send(command);
            return CreateActionResultInstance(result);
        }

        [Authorize(Roles = "SuperAdmin,Admin")]
        [HttpPost("ship")]
        public async Task<IActionResult> ShipProduct(ShipProduct command)
        {
            var result = await _mediator.Send(command);
            return CreateActionResultInstance(result);
        }

        [Authorize(Roles = "SuperAdmin,Admin,User")]
        [HttpGet("all/{pagesize?}/{pagenumber?}")]
        public async Task<IActionResult> AllProducts(int pagesize, int pagenumber, [FromQuery] DateTime? startdate, DateTime? enddate, string? search)
        {
            var result = await _mediator.Send(new ListProduct()
            {
                PageNumber = pagenumber,
                PageSize = pagesize,
                SearchTerm = search,
                StartDate = startdate,
                EndDate = enddate
            });
            return CreateActionResultInstance(result);
        }

        [Authorize(Roles = "SuperAdmin,Admin,User")]
        [HttpGet("summary/{pagesize?}/{pagenumber?}")]
        public async Task<IActionResult> GetSum(int pagesize, int pagenumber, [FromQuery] DateTime? startdate, DateTime? enddate, Guid? id)
        {
            var result = await _mediator.Send(new ListProductByUnit()
            {
                PageNumber = pagenumber,
                Pagesize = pagesize,
                Id = id,
                StartTime = startdate,
                EndTime = enddate
            });
            return CreateActionResultInstance(result);
        }

        //[Authorize(Roles = "SuperAdmin,Admin,User")]
        [HttpPost("export")]
        public async Task<IActionResult> RequestExcelExport([FromBody] RequestExcelExport request)
        {
            var result = await _mediator.Send(request);
            return CreateActionResultInstance(result);
        }

        [Authorize(Roles = "SuperAdmin,Admin,User")]
        [HttpGet("download/{correlationId}")]
        public async Task<IActionResult> DownloadExcel(Guid correlationId)
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            var result = await _mediator.Send(new DownloadExcelQuery
            {
                CorrelationId = correlationId,
                UserId = userId
            });

            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result.Errors);
            }

            return File(result.Data.Content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", result.Data.FileName);
        }
    }
}
