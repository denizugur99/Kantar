using Kantarv2.Enums;

namespace Kantarv2.Messages
{
    public record ExcelExportMessage
    {
        public Guid CorrelationId { get; init; } = Guid.NewGuid();
        public Guid UserId { get; init; }
        public string UserName { get; init; } = string.Empty;
        public ExcelExportType ExportType { get; init; }
        public ExcelExportFilters Filters { get; init; } = new();
        public DateTime RequestedAt { get; init; } = DateTime.UtcNow;
    }

    public record ExcelExportFilters
    {
        public DateTime? StartDate { get; init; }
        public DateTime? EndDate { get; init; }
        public string? SearchTerm { get; init; }
    }
}
