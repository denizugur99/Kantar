using Kantarv2.Dtos;
using MediatR;

namespace Kantarv2.Queries.User
{
    public class ListAllQuery: IRequest<Response<List<UserDto>>>
    {
        public string? Search { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        }
}
