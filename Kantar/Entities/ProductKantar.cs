namespace Kantar.Entities
{
    public class ProductKantar
    {
        public Guid Id { get; set; }    
        public double Weight { get; set; }
        public DateTime DateTime { get; set; }  
        public UnitPrice UnitPrice { get; set; }
        public double TotalPrice{ get; set; }
        public double Devir { get; set; } 
        public bool IsDeleted { get; set; }

    }
}
