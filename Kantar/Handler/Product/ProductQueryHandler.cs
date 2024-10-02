using Kantar.DAL;
using Kantar.Dtos;
using Kantar.Pagination;
using Kantar.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kantar.Handler.Product
{
    public class ProductQueryHandler:IRequestHandler<ProductsQuery,Response<ProductQueryDto>>,
                                     IRequestHandler<GetProductsWithTimeQuery, Response<ProductQueryDto>>
    {
        private readonly KantarDbContext _context;

        public ProductQueryHandler(KantarDbContext context)
        {
            _context = context;
        }

        public async Task<Response<ProductQueryDto>> Handle(ProductsQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var query = _context.Products.Include(x=>x.UnitPrice).Where(x => !x.IsDeleted);
                if(query == null) {
                    return Response<ProductQueryDto>.Fail("ÜRÜN YOK", 500);
                }
                int pagesize=request.PageSize;
                int page=request.PageCount;
                var products = await query
                    .Skip((page - 1) * pagesize)
                    .Take(pagesize).Select(u => new ProductDto()
                     {
                        Name = u.UnitPrice.Name,
                        Weight = u.Weight,
                        DateTime = u.DateTime.Date,
                        TotalPrice = u.TotalPrice,

                    }).ToListAsync();
                var totalRecords = query.Count();
                double x;

                var devir =await query.GroupBy(x => x.UnitPrice).Select(y=>new Information()
                {
                    Name=y.Key.Name,
                    devir=y.Sum(x=>x.TotalPrice),
                    totalweight=y.Sum(x=>x.Weight)

                }
                ).ToListAsync();
                
                PaginationMaker pagination = new PaginationMaker()
                {
                    PageSize = pagesize,
                    PageNumber = page,
                    TotalRecords = totalRecords
                };
                ProductQueryDto queryDto = new ProductQueryDto();
                queryDto.Products=products;
                queryDto.ınformations = devir;
                
                return Response<ProductQueryDto>.Success(queryDto, 200,pagination);
               

            }
            catch (Exception)
            {

                return Response<ProductQueryDto>.Fail("HATA", 500);
            }
        }

        public async Task<Response<ProductQueryDto>> Handle(GetProductsWithTimeQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var pagesize=request.pageSize;
                var pagenumber=request.pageNumber;
                var query = _context.Products.Include(x => x.UnitPrice).Where(x => (!x.IsDeleted) && (x.DateTime >= request.FirstDate) && (x.DateTime <= request.LastDate) &&
                (string.IsNullOrEmpty(request.search) || x.UnitPrice.Name.Trim().ToLower().Contains(request.search.Trim().ToLower())));
                if (query == null)
                {
                    return Response<ProductQueryDto>.Fail("ÜRÜN YOK", 500);
                }
                var products= await query.Skip((pagenumber - 1) * pagesize).Take(pagesize).Select(u => new ProductDto
                {
                    Name = u.UnitPrice.Name,
                    Weight = u.Weight,
                    DateTime = u.DateTime.Date,
                }).ToListAsync();
                var totalRecords = query.Count();
                PaginationMaker pagination = new PaginationMaker()
                {
                    PageSize = pagesize,
                    PageNumber = pagenumber,
                    TotalRecords = totalRecords
                };
                List<ProductQueryDto> result = new List<ProductQueryDto>();
                var devir = await query.GroupBy(x => x.UnitPrice).Select(y => new Information()
                {
                    Name = y.Key.Name,
                    devir = y.Sum(x => x.TotalPrice),
                    totalweight = y.Sum(x => x.Weight)

                }
                ).ToListAsync();
                ProductQueryDto queryDto = new ProductQueryDto();
                queryDto.Products = products;
                queryDto.ınformations = devir;

                return Response<ProductQueryDto>.Success(queryDto, 200,pagination);


            }
            catch (Exception)
            {

                return Response<ProductQueryDto>.Fail("HATA", 500);
            }
        }
    }
}
