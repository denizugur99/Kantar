using Kantarv2.Dtos;
using Kantarv2.Enums;
using MediatR;

namespace Kantarv2.Queries.Products
{
    public class RequestExcelExport : IRequest<Response<ExcelExportResponse>>
    {
        public ExcelExportType ExportType { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? SearchTerm { get; set; }
    }

    public class ExcelExportResponse
    {
        public Guid CorrelationId { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
