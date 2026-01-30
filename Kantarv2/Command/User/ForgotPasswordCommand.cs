using Kantarv2.Dtos;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Kantarv2.Command.User
{
    public class ForgotPasswordCommand : IRequest<Response<NoContent>>
    {
        public string Email { get; set; } = string.Empty;
    }
}
