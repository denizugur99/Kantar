using Kantar.Dtos;
using Kantar.Entities;
using MediatR;

namespace Kantar.Command
{
    public class AddUnitPriceCommand:IRequest<Response<UnitPriceDto>>
    {
        public string Name { get; set; }
        public double Prize { get; set; }
    }
}
