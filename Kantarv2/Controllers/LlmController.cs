using Kantarv2.Queries.Llm;
using MediatR;
using Microsoft.AspNetCore.Authorization;
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
        [HttpGet("ask")]
        public async Task<IActionResult> Ask([FromQuery] string prompt)
        {
            var result = await _mediator.Send(new AskLlmQuery
            {
                Prompt = prompt
            });

            return CreateActionResultInstance(result);
        }
    }
}
