namespace Kantar.Entities
{
    public class UnitPrice
    {
        public Guid Id { get; set; }
        public double Price { get; set; }
        public string Name { get; set; }
        public bool IsDeleted { get; set; }
    }
}
