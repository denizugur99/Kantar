using Kantarv2.Dtos;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Kantarv2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BaseController : ControllerBase
    {
        public IActionResult CreateActionResultInstance<T>(Dtos.Response<T> response)
        {
            if (response.StatusCode == 204)
            {
                return NoContent();
            }
            return new ObjectResult(response)
            {
                StatusCode = response.StatusCode
            };
        }

        public IActionResult CreateActionResultInstance<T>(ValidationResult validationResult)
        {
            return new ObjectResult(Response<T>.Fail(validationResult, 400))
            {
                StatusCode = 400
            };
        }
    }
}