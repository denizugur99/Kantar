using Kantarv2.Dtos;
using MediatR;

namespace Kantarv2.Queries.User
{
    public class FindUserById:IRequest<Response<UserDto>>
    {
        public Guid Id { get; set; }
    }
}
