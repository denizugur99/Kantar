using Kantar.DAL;
using Kantar.Dtos;
using Kantar.Queries;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Runtime.CompilerServices;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace Kantar.Handler.Price
{
    public class PriceQueryHandler:IRequestHandler<PriceQuery, Response<List<UnitPriceWithIdDto>>>
    {
        private readonly KantarDbContext _context;

        public PriceQueryHandler(KantarDbContext context)
        {
            _context = context;
        }

        public async Task<Response<List<UnitPriceWithIdDto>>> Handle(PriceQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var query = await _context.UnitPrice.Where(w => !w.IsDeleted).Select(u => new UnitPriceWithIdDto
                {
                    Id = u.Id,
                    Price = u.Price,
                    ProductName = u.Name
                }).ToListAsync();
                return Response<List<UnitPriceWithIdDto>>.Success(query, 200);
            }
            catch (Exception)
            {

                return Response<List<UnitPriceWithIdDto>>.Fail("Hata", 500);
            }
           
        }
    }
}
