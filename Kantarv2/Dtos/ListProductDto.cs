using Kantarv2.Enums;

namespace Kantarv2.Dtos
{
    public class ListProductDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public ProductStatus Status { get; set; }
        public string Kilogram { get; set; }
        public double Price { get; set; }
        public double TotalPrice { get; set; }
        public DateTime CreatedDate { get; set; }

    }
}
