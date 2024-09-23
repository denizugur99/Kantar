using Kantar.Command;
using Kantar.DAL;
using Kantar.Dtos;
using Kantar.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Kantar.Handler.Price
{
    public class PriceCommandHandler : IRequestHandler<UpdatePrizeCommand, Response<UnitPriceDto>>,
                                     IRequestHandler<AddUnitPriceCommand, Response<UnitPriceDto>>
    {
        private readonly KantarDbContext _context;

        public PriceCommandHandler(KantarDbContext context)
        {
            _context = context;
        }

        public async Task<Response<UnitPriceDto>> Handle(UpdatePrizeCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var query = _context.UnitPrice.Where(x => !x.IsDeleted).FirstOrDefault(x => x.Id.Equals(request.Id));
                if (query == null)
                {
                    return Response<UnitPriceDto>.Fail("bu idde bulunan bir ürün yok", 500);
                }
                query.Price = request.Price;
                UnitPriceDto result = new UnitPriceDto()
                {
                    ProductName = query.Name,
                    Price = query.Price
                };
                _context.SaveChanges();
                return Response<UnitPriceDto>.Success(result, 200);


            }
            catch (Exception)
            {

                return Response<UnitPriceDto>.Fail("Hata", 500);
            }

        }
        public async Task<Response<UnitPriceDto>> Handle(AddUnitPriceCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var query = _context.UnitPrice.Where(x => !x.IsDeleted);
                foreach (var item in query)
                {
                    if (item.Name.Trim().ToLower().Equals(request.Name.Trim().ToLower()))
                        return Response<UnitPriceDto>.Fail("Bu adda ürün zaten var fiyatı updatelemeyi deneyin", 500);
                }
                var unitPrize = new UnitPrice()
                {
                    Name = request.Name.Trim().ToLower(),
                    Price = request.Prize
                };
                await _context.UnitPrice.AddAsync(unitPrize);
                await _context.SaveChangesAsync();
                return Response<UnitPriceDto>.Success(204);
            }
            catch (Exception)
            {
                return Response<UnitPriceDto>.Fail("hata", 500);
            }

        }
    }
}
