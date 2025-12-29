using Kantarv2.Dtos;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Kantarv2.Command.User
{
    public class LogoutCommand : IRequest<Response<NoContent>>
    {
        public string RefreshToken { get; set; }
        public int UserId { get; set; }
    }
}
