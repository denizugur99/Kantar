using Kantarv2.Queries.Llm;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Kantarv2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LlmController : BaseController
    {
        private readonly IMediator _mediator;
        public LlmController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [Authorize(Roles = "SuperAdmin,Admin,User")]
        [HttpGet("analyze")]
        public async Task<IActionResult> AnalyzeProduct([FromQuery] string prompt)
        {
            var result = await _mediator.Send(new Productaskllmquery
            {
                Prompt = prompt
            });

            return CreateActionResultInstance(result);
        }
    }
}
