using Kantarv2.Dtos;
using MediatR;

namespace Kantarv2.Queries.User
{
    public class FindUserById:IRequest<Response<UserDto>>
    {
        public int Id { get; set; }
    }
}
