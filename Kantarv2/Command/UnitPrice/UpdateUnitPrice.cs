using Kantarv2.Dtos;
using MediatR;

namespace Kantarv2.Command.UnitPrice
{
    public class UpdateUnitPrice:IRequest<Response<UnitPriceDto>>
    {
        public Guid Id { get; set; }
        public double Price { get; set; }
    }
}
