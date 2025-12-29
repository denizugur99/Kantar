using Kantarv2.Dtos;
using MediatR;

namespace Kantarv2.Queries.Products
{
    public class ProductsWithExcel: IRequest<ExportExcelDto>
    {
        public string? SearchTerm { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}
