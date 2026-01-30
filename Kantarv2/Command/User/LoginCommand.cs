using Kantarv2.Dtos;
using MediatR;
namespace Kantarv2.Command.User
{
    public class LoginCommand:IRequest<Response<TokenDto>>
{
        public string Username { get; set; }
        public string Password { get; set; }
}

}