using Kantar.Dtos;
using MediatR;

namespace Kantar.Queries
{
    public class ProductsQuery:IRequest<Response<List<ProductQueryDto>>>
    {
    }
}
