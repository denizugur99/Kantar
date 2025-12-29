using Kantarv2.Dtos;
using MediatR;

namespace Kantarv2.Command.User
{
    public class RefreshTokenCommand:IRequest<Response<TokenDto>>
    {
        public int UserId { get; set; }
        public string RefreshToken { get; set; }
    }
}
