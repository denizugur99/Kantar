using Kantar.DAL;
using Kantar.Dtos;
using Kantar.Queries;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Kantar.Handler.Product
{
    public class ProductQueryHandler:IRequestHandler<ProductsQuery,Response<List<ProductQueryDto>>>,
                                     IRequestHandler<GetProductsWithTimeQuery, Response<List<ProductQueryDto>>>
    {
        private readonly KantarDbContext _context;

        public ProductQueryHandler(KantarDbContext context)
        {
            _context = context;
        }

        public async Task<Response<List<ProductQueryDto>>> Handle(ProductsQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var query = await _context.Products.Include(x=>x.UnitPrice).Where(x => !x.IsDeleted).Select(u => new ProductQueryDto
                {
                    Name = u.UnitPrice.Name,
                    Weight = u.Weight,
                    DateTime = u.DateTime.Date,
                    TotalPrice = u.TotalPrice,
                    Devir=u.Devir

                }).ToListAsync();
                if(query == null) {
                    return Response<List<ProductQueryDto>>.Fail("ÜRÜN YOK", 500);
                }
                return Response<List<ProductQueryDto>>.Success(query, 200);
               

            }
            catch (Exception)
            {

                return Response<List<ProductQueryDto>>.Fail("HATA", 500);
            }
        }

        public async Task<Response<List<ProductQueryDto>>> Handle(GetProductsWithTimeQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var query = await _context.Products.Include(x => x.UnitPrice).Where(x => (!x.IsDeleted)&&(x.DateTime>=request.FirstDate)&&(x.DateTime<=request.LastDate)  ).Select(u => new ProductQueryDto
                {
                    Name = u.UnitPrice.Name,
                    Weight = u.Weight,
                    DateTime = u.DateTime.Date,
                    TotalPrice = u.TotalPrice,
                    Devir = u.Devir

                }).ToListAsync();
                if (query == null)
                {
                    return Response<List<ProductQueryDto>>.Fail("ÜRÜN YOK", 500);
                }
                return Response<List<ProductQueryDto>>.Success(query, 200);


            }
            catch (Exception)
            {

                return Response<List<ProductQueryDto>>.Fail("HATA", 500);
            }
        }
    }
}
