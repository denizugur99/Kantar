using Kantar.DAL;
using Kantar.Dtos;
using Kantar.Pagination;
using Kantar.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
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
                var query = _context.Products.Include(x=>x.UnitPrice).Where(x => !x.IsDeleted);
                if(query == null) {
                    return Response<List<ProductQueryDto>>.Fail("ÜRÜN YOK", 500);
                }
                int pagesize=request.PageSize;
                int page=request.PageCount;
                var products = await query
                    .Skip((page - 1) * pagesize)
                    .Take(pagesize).Select(u => new ProductQueryDto()
                     {
                        Name = u.UnitPrice.Name,
                        Weight = u.Weight,
                        DateTime = u.DateTime.Date,
                        TotalPrice = u.TotalPrice,
                        Devir = u.Devir

                    }).ToListAsync();
                var totalRecords = query.Count();

                PaginationMaker pagination = new PaginationMaker()
                {
                    PageSize = pagesize,
                    PageNumber = page,
                    TotalRecords = totalRecords
                };
                return Response<List<ProductQueryDto>>.Success(products, 200,pagination);
               

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
                var pagesize=request.pageSize;
                var pagenumber=request.pageNumber;
                var query = await _context.Products.Include(x => x.UnitPrice).Where(x => (!x.IsDeleted)&&(x.DateTime>=request.FirstDate)&&(x.DateTime<=request.LastDate)  ).Skip((pagenumber - 1) * pagesize).Take(pagesize).Select(u => new ProductQueryDto
                {
                    Name = u.UnitPrice.Name,
                    Weight = u.Weight,
                    DateTime = u.DateTime.Date,
                    TotalPrice = u.TotalPrice,
                    Devir = u.Devir

                }).ToListAsync();
                var totalRecords = query.Count();
                PaginationMaker pagination = new PaginationMaker()
                {
                    PageSize = pagesize,
                    PageNumber = pagenumber,
                    TotalRecords = totalRecords
                };

                if (query == null)
                {
                    return Response<List<ProductQueryDto>>.Fail("ÜRÜN YOK", 500);
                }
                return Response<List<ProductQueryDto>>.Success(query, 200,pagination);


            }
            catch (Exception)
            {

                return Response<List<ProductQueryDto>>.Fail("HATA", 500);
            }
        }
    }
}
