using Kantar.Dtos;
using MediatR;

namespace Kantar.Queries
{
    public class ProductsQuery:IRequest<Response<ProductQueryDto>>
    {
        public int PageSize { get; set; }
        public int PageCount { get; set; }
    }
}
