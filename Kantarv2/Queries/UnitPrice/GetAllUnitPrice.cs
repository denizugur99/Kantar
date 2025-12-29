using Kantarv2.Dtos;
using MediatR;

namespace Kantarv2.Queries.UnitPrice
{
    public class GetAllUnitPrice:IRequest<Response<List<UnitPriceDto>>>
    {
        public int PageNumber { get; set; } 
        public int PageSize { get; set; } 
        public string? SearchTerm { get; set; }
    }
}
