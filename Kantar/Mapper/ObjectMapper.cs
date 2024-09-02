using AutoMapper;
using Kantar.Command;
using Kantar.Entities;

namespace Kantar.Mapper
{
    public class ObjectMapper
    {
        private static readonly Lazy<IMapper> lazy = new Lazy<IMapper>(() =>
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<CustomMapping>();

            });
            return config.CreateMapper();
        });
        public static IMapper Mapper => lazy.Value;
    }
    internal class CustomMapping : Profile
    {
        public CustomMapping()
        {
            CreateMap<AddProductCommand, ProductKantar>().ReverseMap();
        }
    }
}
