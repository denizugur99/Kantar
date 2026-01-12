using Kantarv2.Dtos;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Kantarv2.Command.User
{
    public class LogoutCommand : IRequest<Response<NoContent>>
    {
        public string RefreshToken { get; set; }

        /// <summary>
        /// User ID - Optional. If not provided (0), will be extracted from JWT token claims.
        /// </summary>
        public int UserId { get; set; } = 0;
    }
}
