using Kantarv2.Queries.Llm;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kantarv2.Controllers
{
    /// <summary>
    /// Yapay zeka (LLM) entegrasyonu - Groq API ile doğal dil işleme
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class LlmController : BaseController
    {
        private readonly IMediator _mediator;
        public LlmController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Yapay zekaya soru sorar ve yanıt alır
        /// </summary>
        /// <param name="prompt">Sorulacak soru veya istek</param>
        /// <returns>Yapay zeka yanıtı</returns>
        /// <response code="200">Yanıt başarıyla alındı</response>
        /// <response code="500">LLM servisi hatası</response>
        /// <remarks>
        /// Model: llama-3.1-8b-instant (Groq API)
        ///
        /// Örnek kullanım:
        /// GET /api/llm/ask?prompt=Bugün hava nasıl?
        /// </remarks>
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
