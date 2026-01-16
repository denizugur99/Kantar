using Kantarv2.DAL;
using Kantarv2.Dtos;
using Kantarv2.Enums;
using Kantarv2.Messages;
using Kantarv2.Pagination;
using Kantarv2.Queries.Products;
using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Kantarv2.Handler.QueryHandler
{
    public class ProductQueryHandler : IRequestHandler<ListProduct, Dtos.Response<List<ListProductDto>>>,
        IRequestHandler<ListProductByUnit, Dtos.Response<List<ListProductByUnitDto>>>,
        IRequestHandler<RequestExcelExport, Dtos.Response<ExcelExportResponse>>,
        IRequestHandler<DownloadExcelQuery, Dtos.Response<ExportExcelDto>>
    {
        private readonly KantarDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly ILogger<ProductQueryHandler> _logger;

        public ProductQueryHandler(
            KantarDbContext context,
            IHttpContextAccessor httpContextAccessor,
            IPublishEndpoint publishEndpoint,
            ILogger<ProductQueryHandler> logger)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _publishEndpoint = publishEndpoint;
            _logger = logger;
        }

        public async Task<Dtos.Response<List<ListProductDto>>> Handle(ListProduct request, CancellationToken cancellationToken)
        {
            try
            {
                var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? "Bilinmeyen Kullanıcı";
                var query = _context.Products.Include(x => x.UnitPrice).Where(p => (!p.IsDeleted) && (string.IsNullOrEmpty(request.SearchTerm) || p.UnitPrice.Name.Trim().ToLower().Contains(request.SearchTerm.Trim().ToLower())));
                var PageSize = request.PageSize;
                var PageNumber = request.PageNumber;
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
                var totalRecords = await query.CountAsync();
                var list = await query.Skip((PageNumber - 1) * PageSize).Take(PageSize).Select(q => new ListProductDto()
                {
                    Id = q.Id,
                    Name = q.UnitPrice.Name,
                    Status = q.Status,
                    Kilogram = q.Weight.ToString() + " kilogram",
                    Price = q.Price,
                    TotalPrice = q.TotalPrice,
                    CreatedDate = q.CreatedDate.Date,
                }).OrderBy(x => x.CreatedDate).ToListAsync(cancellationToken);

                PaginationMaker pagination = new PaginationMaker()
                {
                    PageSize = PageSize,
                    PageNumber = PageNumber,
                    TotalRecords = totalRecords,
                };
                if (list.Count == 0)
                {
                    _logger.LogWarning("No products has found");
                    return Dtos.Response<List<ListProductDto>>.Fail(400, "No products has found");
                }
                else
                {
                    _logger.LogInformation($"Produtcs has listed by {userId} ");
                    return Dtos.Response<List<ListProductDto>>.Success(200, list, pagination);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"{ex.Message}");
                return Dtos.Response<List<ListProductDto>>.Fail(500, ex.Message);
            }
        }

        public async Task<Dtos.Response<List<ListProductByUnitDto>>> Handle(ListProductByUnit request, CancellationToken cancellationToken)
        {
            try
            {
                var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? "Bilinmeyen Kullanıcı";
                var query = _context.Products.Include(x => x.UnitPrice).Where(x => !x.IsDeleted);
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
                var records = await query.GroupBy(x => x.UnitPrice).Skip((PageNumber - 1) * PageSize).Take(PageSize).Select(x => new ListProductByUnitDto()
                {
                    Name = x.Key.Name,
                    TotalWeight = x.Sum(s => s.Weight).ToString() + " kg",
                    TotalPrice = x.Sum(p => p.TotalPrice).ToString() + " TL"
                }).ToListAsync(cancellationToken);
                var totalRecords = records.Count();
                PaginationMaker pagination = new PaginationMaker()
                {
                    PageSize = PageSize,
                    PageNumber = PageNumber,
                    TotalRecords = totalRecords
                };

                _logger.LogInformation($"Products by unit has listed by {userId}");

                return Dtos.Response<List<ListProductByUnitDto>>.Success(200, records, pagination);
            }
            catch (Exception)
            {
                _logger.LogError("An error occurred while listing products by unit.");
                return Dtos.Response<List<ListProductByUnitDto>>.Fail(500, "An error occurred while listing products by unit.");
            }
        }

        public async Task<Dtos.Response<ExcelExportResponse>> Handle(RequestExcelExport request, CancellationToken cancellationToken)
        {
            try
            {
                var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userName = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";

                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                {
                    return Dtos.Response<ExcelExportResponse>.Fail(401, "User not authenticated");
                }

                var message = new ExcelExportMessage
                {
                    UserId = userId,
                    UserName = userName,
                    ExportType = request.ExportType,
                    Filters = new ExcelExportFilters
                    {
                        StartDate = request.StartDate,
                        EndDate = request.EndDate,
                        SearchTerm = request.SearchTerm
                    }
                };

                await _publishEndpoint.Publish(message, cancellationToken);

                _logger.LogInformation(
                    "Excel export request queued. CorrelationId={CorrelationId}, UserId={UserId}, ExportType={ExportType}",
                    message.CorrelationId,
                    userId,
                    request.ExportType);

                return Dtos.Response<ExcelExportResponse>.Success(202, new ExcelExportResponse
                {
                    CorrelationId = message.CorrelationId,
                    Message = "Excel export request has been queued. You will be notified when it's ready."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to queue excel export request");
                return Dtos.Response<ExcelExportResponse>.Fail(500, "Failed to queue excel export request");
            }
        }

        public Task<Dtos.Response<ExportExcelDto>> Handle(DownloadExcelQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var exportPath = Path.Combine(Directory.GetCurrentDirectory(), "Exports", request.UserId.ToString());

                if (!Directory.Exists(exportPath))
                {
                    _logger.LogWarning("Export folder not found for user {UserId}", request.UserId);
                    return Task.FromResult(Dtos.Response<ExportExcelDto>.Fail(404, "Export folder not found"));
                }

                var files = Directory.GetFiles(exportPath, $"{request.CorrelationId}_*.xlsx");

                if (files.Length == 0)
                {
                    _logger.LogWarning("File not found. CorrelationId={CorrelationId}", request.CorrelationId);
                    return Task.FromResult(Dtos.Response<ExportExcelDto>.Fail(404, "File not found"));
                }

                var filePath = files[0];
                var fileName = Path.GetFileName(filePath).Replace($"{request.CorrelationId}_", "");
                var fileBytes = File.ReadAllBytes(filePath);

                _logger.LogInformation("Excel file downloaded. CorrelationId={CorrelationId}, UserId={UserId}",
                    request.CorrelationId, request.UserId);

                return Task.FromResult(Dtos.Response<ExportExcelDto>.Success(200, new ExportExcelDto
                {
                    FileName = fileName,
                    Content = fileBytes
                }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to download excel file. CorrelationId={CorrelationId}", request.CorrelationId);
                return Task.FromResult(Dtos.Response<ExportExcelDto>.Fail(500, "Failed to download file"));
            }
        }
    }
}
