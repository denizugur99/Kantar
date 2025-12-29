using Kantarv2.Dtos;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Kantarv2.Command.Product
{
    public class DeleteProduct:IRequest<Response<NoContent>>
    {
        public Guid Id { get; set; }
    }
}
