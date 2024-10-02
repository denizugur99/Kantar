namespace Kantar.Entities
{
    public class ProductKantar
    {
        public Guid Id { get; set; }    
        public double Weight { get; set; }
        public DateTime DateTime { get; set; }  
        public Guid unitPriceId { get; set; }
        public UnitPrice UnitPrice { get; set; }
        public double TotalPrice{ get; set; }
        public bool IsDeleted { get; set; }

    }
}
