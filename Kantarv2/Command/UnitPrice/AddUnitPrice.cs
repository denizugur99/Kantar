using Kantarv2.Dtos;
using MediatR;

namespace Kantarv2.Command.UnitPrice
{
    public class AddUnitPrice:IRequest<Response<UnitPriceDto>>
    {
        public double Price { get; set; }
        public string Name { get; set; }
        
    }
}
