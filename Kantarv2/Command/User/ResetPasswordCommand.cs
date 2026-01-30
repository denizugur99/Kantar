using Kantarv2.Dtos;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Kantarv2.Command.User
{
    public class ResetPasswordCommand : IRequest<Response<NoContent>>
    {
        public string Email { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }
}
