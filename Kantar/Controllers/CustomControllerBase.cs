using Kantar.Dtos;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Kantar.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CustomControllerBase : ControllerBase
    {
        public IActionResult CreateActionResultInstance<T>(Response<T> response)
        {
            if (response.StatusCode == 204)
                return NoContent();
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

        [ApiExplorerSettings(IgnoreApi = true)]
        public string GetUserId()
        {
            if (HttpContext.User.Claims.Any())
                return HttpContext.User.Claims.First(f => f.Type.Equals("userid")).Value;
            else return null;
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpGet("getusername")]
        public string GetUserName()
        {
            if (HttpContext.User.Claims.Any())
                return HttpContext.User.Claims.First(f => f.Type.Equals("name")).Value;
            else return null;
        }


    }
}

