namespace Kantarv2.Dtos
{
    public class AddProductDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public double Price { get; set; }
        public double Weight { get; set; }
        public double TotalPrice { get; set; }
        
    }
}
