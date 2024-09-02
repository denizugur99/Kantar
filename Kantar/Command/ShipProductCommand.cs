using Kantar.Dtos;
using Kantar.Entities;
using MediatR;

namespace Kantar.Command
{
    public class ShipProductCommand:IRequest<Response<NoContent>>
    {
        public double Price { get; set; }
        public string ProductName { get; set; }
        public double Weight { get; set; }
    }
}
