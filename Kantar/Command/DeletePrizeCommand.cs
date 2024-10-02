using Kantar.Dtos;
using Kantar.Entities;
using MediatR;

namespace Kantar.Command
{
    public class DeletePrizeCommand : IRequest<Response<NoContent>>
    {
        public Guid Id { get; set; }
    }
}
