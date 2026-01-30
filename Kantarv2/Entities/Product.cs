using Kantarv2.Enums;

namespace Kantarv2.Entities
{
    public class Product
    {
        public Guid Id { get; set; }
        public double Weight { get; set; }
        public double Price { get; set; }
        public DateTime CreatedDate { get; set; }
        public UnitPrice UnitPrice { get; set; }
        public double TotalPrice { get; set; }

       public ProductStatus Status { get; set; }
        public bool IsDeleted { get; set; }

    }
}
