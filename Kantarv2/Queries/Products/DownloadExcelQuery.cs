using Kantarv2.Dtos;
using MediatR;

namespace Kantarv2.Queries.Products
{
    public class DownloadExcelQuery : IRequest<Response<ExportExcelDto>>
    {
        public Guid CorrelationId { get; set; }
        public Guid UserId { get; set; }
    }
}
