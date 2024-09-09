using Kantar.DAL;
using Kantar.Dtos;
using Kantar.Pagination;
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
                var query = _context.UnitPrice.Where(w => (!w.IsDeleted)&&
                (string.IsNullOrEmpty(request.search)||(w.Name.Trim().ToLower().Contains(request.search.Trim().ToLower()))));
                var totalRecords=query.Count();
                var pageNumber=request.pageNumber;
                var pageSize=request.pageSize;
                var units=await query
                    .Skip((pageNumber-1)*pageSize)
                    .Take(pageSize)
                    .Select(u => new UnitPriceWithIdDto
                    {
                        Id = u.Id,
                        Price = u.Price,
                        ProductName = u.Name
                    }).ToListAsync();
                PaginationMaker pagination=new PaginationMaker()
                {
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalRecords = totalRecords
                };
                return Response<List<UnitPriceWithIdDto>>.Success(units, 200,pagination);
            }
            catch (Exception)
            {

                return Response<List<UnitPriceWithIdDto>>.Fail("Hata", 500);
            }
           
        }
    }
}
