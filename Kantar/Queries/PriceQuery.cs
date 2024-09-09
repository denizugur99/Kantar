using Kantar.Dtos;
using MediatR;
using MediatR.NotificationPublishers;

namespace Kantar.Queries
{
    public class PriceQuery : IRequest<Response<List<UnitPriceWithIdDto>>>
    {
        public int pageSize { get; set; }
        public int pageNumber { get; set; }
        public string? search {  get; set; }

    }
        
    
}
