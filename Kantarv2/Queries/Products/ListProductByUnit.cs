using Kantarv2.Dtos;
using MediatR;

namespace Kantarv2.Queries.Products
{
    public class ListProductByUnit:IRequest<Response<List<ListProductByUnitDto>>>
    {
        public int Pagesize { get; set; }
        public int PageNumber { get; set; }
        public Guid? Id { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }



    }
}
