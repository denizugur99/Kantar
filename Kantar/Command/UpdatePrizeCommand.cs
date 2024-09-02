using Kantar.Dtos;
using MediatR;

namespace Kantar.Command
{
    public class UpdatePrizeCommand:IRequest<Response<UnitPriceDto>>
    {
        public Guid Id { get; set; }
        public double Price { get; set; }
    }
}
