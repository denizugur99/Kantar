using Kantarv2.Dtos;
using MediatR;

namespace Kantarv2.Command.Product
{
    public class AddProduct:IRequest<Response<AddProductDto>>
    {
        public double Weight { get; set; }
        public Guid UnitId { get; set; }
        public double? Price { get; set; }
       
    }
}
