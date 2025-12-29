using Kantarv2.Dtos;
using MediatR;

namespace Kantarv2.Queries.Products
{
    public class ListProduct:IRequest<Response<List< ListProductDto>>>
    {
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public string? SearchTerm { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

    }
}
