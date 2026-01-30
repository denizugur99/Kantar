using Kantarv2.Dtos;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Kantarv2.Command.User
{
    public class DeleteUser:IRequest<Response<NoContent>>
    {
        public Guid Id { get; set; }
        
    }
}
