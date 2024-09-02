using Kantar.Entities;

namespace Kantar.Dtos
{
    public class ProductQueryDto
    {
        public string Name { get; set; }
        public double Weight { get; set; }
        public DateTime DateTime { get; set; }
        public double TotalPrice { get; set; }
        public double Devir { get; set; }
    }
}
