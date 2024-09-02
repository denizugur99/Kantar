using Kantar.Dtos;
using MediatR;

namespace Kantar.Queries
{
    public class GetProductsWithTimeQuery:IRequest<Response<List<ProductQueryDto>>>
    {
        public DateTime FirstDate { get; set; }
        public DateTime LastDate { get; set; }

    }
}
