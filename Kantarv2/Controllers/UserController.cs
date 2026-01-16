using Kantarv2.Command.User;
using Kantarv2.Queries;
using Kantarv2.Queries.User;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Runtime.CompilerServices;

namespace Kantarv2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : BaseController


    {
        private readonly IMediator _mediator;
        public UserController(IMediator mediator)
        {
            _mediator = mediator;
        }
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginCommand command)
        {
            var result = await _mediator.Send(command);
            return CreateActionResultInstance(result);
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterCommand command)
        {
            var result = await _mediator.Send(command);
            return CreateActionResultInstance(result);
        }
        [HttpPost("refreshtoken")]
        public async Task<IActionResult> RefreshToken( RefreshTokenCommand command)
        {
            var result = await _mediator.Send(command);
            return CreateActionResultInstance(result);
        }
        [Authorize(Roles = "SuperAdmin")]
        [HttpGet("all/{pagenumber?}/{pagesize?}")]
        public async Task<IActionResult> GetAll(int pagenumber,int pagesize ,[FromQuery] string? search) {
            var result = await _mediator.Send(new ListAllQuery()
            {
                PageNumber = pagenumber,
                PageSize = pagesize,
                Search = search
            });
            return CreateActionResultInstance(result);
        }
        [Authorize(Roles = "SuperAdmin")]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _mediator.Send(new FindUserById { Id = id });
            return CreateActionResultInstance(result);
        }
        [Authorize(Roles = "SuperAdmin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(Guid id)
        {
            var result = await _mediator.Send( new DeleteUser { Id = id });
            return CreateActionResultInstance(result);
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout(LogoutCommand command)
        {
            var result = await _mediator.Send(command);
            return CreateActionResultInstance(result);
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordCommand command)
        {
            var result = await _mediator.Send(command);
            return CreateActionResultInstance(result);
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(ResetPasswordCommand command)
        {
            var result = await _mediator.Send(command);
            return CreateActionResultInstance(result);
        }
    }
}
