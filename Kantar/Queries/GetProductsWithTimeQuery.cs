using Kantar.Dtos;
using MediatR;

namespace Kantar.Queries
{
    public class GetProductsWithTimeQuery:IRequest<Response<List<ProductQueryDto>>>
    {
        public int pageNumber { get; set; }
        public int pageSize { get; set; }
        public DateTime FirstDate { get; set; }
        public DateTime LastDate { get; set; }
        public string? search {  get; set; }

    }
}
