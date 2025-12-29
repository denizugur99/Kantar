using Kantarv2.DAL;
using Kantarv2.Dtos;
using Kantarv2.Pagination;
using Kantarv2.Queries.Products;
using Kantarv2.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Kantarv2.Handler.QueryHandler

{
    public class ProductQueryHandler : IRequestHandler<ListProduct, Response<List<ListProductDto>>>,
        IRequestHandler<ListProductByUnit,Response<List<ListProductByUnitDto>>>,
        IRequestHandler<GetWithExcel,ExportExcelDto>,
        IRequestHandler<ProductsWithExcel, ExportExcelDto>
    {
        private readonly KantarDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IExcelServiceInterface _excelService;
        private readonly ILogger<ProductQueryHandler> _logger;

        public ProductQueryHandler(KantarDbContext context,IHttpContextAccessor httpContextAccessor,IExcelServiceInterface excelService ,ILogger<ProductQueryHandler> logger)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _excelService = excelService;
            _logger = logger;
        }

        public async Task<Response<List<ListProductDto>>> Handle(ListProduct request, CancellationToken cancellationToken)
        {
            try
            {
                var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? "Bilinmeyen Kullanıcı";
                var query = _context.Products.Include(x => x.UnitPrice).Where(p => (!p.IsDeleted) && (string.IsNullOrEmpty(request.SearchTerm) || p.UnitPrice.Name.Trim().ToLower().Contains(request.SearchTerm.Trim().ToLower())));
                var PageSize=request.PageSize;
                var PageNumber=request.PageNumber;
                if(request.StartDate.HasValue)
                {
                    var startDate = request.StartDate.Value.Date;
                    query=query.Where(q=>q.CreatedDate>=startDate);
                   
                }
                if (request.EndDate.HasValue) {
                    var EndDate = request.EndDate.Value.Date.AddDays(1);
                    query=query.Where(q=>q.CreatedDate<=EndDate);
                }
                var totalRecords=await query.CountAsync();
                var list=await query.Skip((PageNumber-1)*PageSize).Take(PageSize).Select(q=>new ListProductDto()
                {
                    Id = q.Id,
                    Name=q.UnitPrice.Name,
                    Status=q.Status,
                    Kilogram=q.Weight.ToString()+" kilogram",
                    Price=q.Price,
                    TotalPrice=q.TotalPrice,
                    CreatedDate=q.CreatedDate.Date,
                }).OrderBy(x=>x.CreatedDate).ToListAsync(cancellationToken);

                PaginationMaker pagination = new PaginationMaker() {
                
                 PageSize=PageSize,
                 PageNumber=PageNumber,
                 TotalRecords=totalRecords,
                };
                if (list.Count==0)
                {
                    _logger.LogWarning("No products has found");
                    return Response<List<ListProductDto>>.Fail(400, "No products has found");
                }
                else
                {
                    _logger.LogInformation($"Produtcs has listed by {userId} ");
                    return Response<List<ListProductDto>>.Success(200, list, pagination);
                }
            }
            catch (Exception ex)
            {

                _logger.LogWarning($"{ex.Message}");
                return Response<List<ListProductDto>>.Fail(500, ex.Message);
            }
        }

        public async Task<Response<List<ListProductByUnitDto>>> Handle(ListProductByUnit request, CancellationToken cancellationToken)
        {
            try
            {
                var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? "Bilinmeyen Kullanıcı";
                var query = _context.Products.Include(x=>x.UnitPrice).Where(x => !x.IsDeleted);
                if (request.StartTime.HasValue)
                {
                    var startDate = request.StartTime.Value.Date;
                    query = query.Where(q => q.CreatedDate >= startDate);

                }
                if (request.EndTime.HasValue)
                {
                    var EndDate = request.EndTime.Value.Date.AddDays(1);
                    query = query.Where(q => q.CreatedDate <= EndDate);
                }
                var PageSize = request.Pagesize;
                var PageNumber = request.PageNumber;
                var records = await query.GroupBy(x => x.UnitPrice).Skip((PageNumber-1)*PageSize).Take(PageSize).Select(x => new ListProductByUnitDto()
                {
                    Name=x.Key.Name,
                    TotalWeight=x.Sum(s=>s.Weight).ToString()+" kg",
                    TotalPrice=x.Sum(p=>p.TotalPrice).ToString()+" TL"

                }).ToListAsync(cancellationToken);
                var totalRecords= records.Count();
                PaginationMaker pagination = new PaginationMaker()
                {
                    PageSize = PageSize,
                    PageNumber = PageNumber,
                    TotalRecords=totalRecords
                };
                
                _logger.LogInformation($"Products by unit has listed by {userId}");

                return Response<List<ListProductByUnitDto>>.Success(200,records,pagination);



            }
            catch (Exception)
            {

                _logger.LogError("An error occurred while listing products by unit.");
                return Response<List<ListProductByUnitDto>>.Fail(500, "An error occurred while listing products by unit.");
            }
        }

        public async Task<ExportExcelDto> Handle(GetWithExcel request, CancellationToken cancellationToken)
        {
            try
            {
                var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? "Bilinmeyen Kullanıcı";
                var query = _context.Products.Include(x => x.UnitPrice).Where(p => (!p.IsDeleted) && (string.IsNullOrEmpty(request.SearchTerm) || p.UnitPrice.Name.Trim().ToLower().Contains(request.SearchTerm.Trim().ToLower())));
                if (request.StartDate.HasValue)
                {
                    var startDate = request.StartDate.Value.Date;
                    query = query.Where(q => q.CreatedDate >= startDate);
                }
                if (request.EndDate.HasValue)
                {
                    var EndDate = request.EndDate.Value.Date.AddDays(1);
                    query = query.Where(q => q.CreatedDate <= EndDate);
                }
                var records = await query.GroupBy(x => x.UnitPrice).Select(x => new ListProductByUnitDto()
                {
                    Name = x.Key.Name,
                    TotalWeight = x.Sum(s => s.Weight).ToString() + " kg",
                    TotalPrice = x.Sum(p => p.TotalPrice).ToString() + " TL"
                }).ToListAsync(cancellationToken);
                string dateRange = "Tum_Zamanlar";
                if (request.StartDate.HasValue && request.EndDate.HasValue)
                {
                    dateRange = $"{request.StartDate.Value:dd_MM_yyyy}_to_{request.EndDate.Value:dd_MM_yyyy}";
                }
                else if (request.StartDate.HasValue)
                {
                    dateRange = $"{request.StartDate.Value:dd_MM_yyyy}_Sonrasi";
                }

                var fileName = $"UrunListesi_{dateRange}.xlsx";

               
                var fileContent =_excelService.GenerateExcel(records, "Ürünlistelesi");
                ExportExcelDto exportExcelDto = new ExportExcelDto()
                {
                    FileName = fileName,
                    Content = fileContent
                };
                _logger.LogInformation($"{fileName} {userId} tarafından başarıyla oluşturuldu.");
                return exportExcelDto;
            }
            catch (Exception ex)
            {
                _logger.LogError($"{ex.Message}");
                return new ExportExcelDto { Content = Array.Empty<byte>(), FileName = "Error.txt" };

            }

        }

        public async Task<ExportExcelDto> Handle(ProductsWithExcel request, CancellationToken cancellationToken)
        {
            try
            {
                var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? "Bilinmeyen Kullanıcı";
                var query = _context.Products.Include(x => x.UnitPrice).Where(p => (!p.IsDeleted) && (string.IsNullOrEmpty(request.SearchTerm) || p.UnitPrice.Name.Trim().ToLower().Contains(request.SearchTerm.Trim().ToLower())));
                if (request.StartDate.HasValue)
                {
                    var startDate = request.StartDate.Value.Date;
                    query = query.Where(q => q.CreatedDate >= startDate);
                }
                if (request.EndDate.HasValue)
                {
                    var EndDate = request.EndDate.Value.Date.AddDays(1);
                    query = query.Where(q => q.CreatedDate <= EndDate);
                }
                var records = query.Select(q => new ListProductDto()
                {
                    Id = q.Id,
                    Name = q.UnitPrice.Name,
                    Status = q.Status,
                    Kilogram = q.Weight.ToString() + " kilogram",
                    Price = q.Price,
                    TotalPrice = q.TotalPrice,
                    CreatedDate = q.CreatedDate.Date,
                }).OrderBy(x => x.CreatedDate).ToList();
                string dateRange = "Tum_Zamanlar";
                if (request.StartDate.HasValue && request.EndDate.HasValue)
                {
                    dateRange = $"{request.StartDate.Value:dd_MM_yyyy}_to_{request.EndDate.Value:dd_MM_yyyy}";
                }
                else if (request.StartDate.HasValue)
                {
                    dateRange = $"{request.StartDate.Value:dd_MM_yyyy}_Sonrasi";
                }

                var fileName = $"UrunListesi_{dateRange}.xlsx";

                var fileContent = _excelService.GenerateExcel(records, "Ürünlistelesi");

                ExportExcelDto exportExcelDto = new ExportExcelDto()
                {
                    FileName = fileName,
                    Content = fileContent
                };
                _logger.LogInformation($"{fileName} {userId} tarafından başarıyla oluşturuldu.");
                return exportExcelDto;

            }
            catch (Exception ex)
            {

                _logger.LogError($"{ex.Message}");
                return new ExportExcelDto { Content = Array.Empty<byte>(), FileName = "Error.txt" };
            }
        }
    }
}
