using Kantarv2.Dtos;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Kantarv2.Command.UnitPrice
{
    public class DeleteUnitPrice:IRequest<Response<NoContent>>
    {
        public Guid Id { get; set; }
    }
}
