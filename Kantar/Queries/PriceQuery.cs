using Kantar.Dtos;
using MediatR;
using MediatR.NotificationPublishers;

namespace Kantar.Queries
{
    public class PriceQuery : IRequest<Response<List<UnitPriceWithIdDto>>>
    {

    }
        
    
}
