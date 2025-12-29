using Kantarv2.DAL;
using Kantarv2.Dtos;
using Kantarv2.Pagination;
using Kantarv2.Queries.UnitPrice;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Kantarv2.Handler.QueryHandler
{
    public class UnitPriceQueryHandler : IRequestHandler<GetAllUnitPrice, Response<List<UnitPriceDto>>>
    {
        private readonly ILogger<UnitPriceQueryHandler> _logger;
        private readonly KantarDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public UnitPriceQueryHandler(IHttpContextAccessor httpContextAccessor, ILogger<UnitPriceQueryHandler> logger, KantarDbContext context)
        {
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
            _context = context;
        }
        public async Task<Response<List<UnitPriceDto>>> Handle(GetAllUnitPrice request, CancellationToken cancellationToken)
        {
            try
            {
                var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? "Bilinmeyen Kullanıcı";
                var query =  _context.UnitPrices.Where(x => (!x.IsDeleted) && (string.IsNullOrEmpty(request.SearchTerm) || (x.Name.Trim().ToLower().Contains(request.SearchTerm.Trim().ToLower()))));
                var totalRecords = await query.CountAsync(cancellationToken);
                var pageNumber = request.PageNumber;
                var pageSize = request.PageSize;

                var Units= await query.Skip((pageNumber-1) * pageSize)
                    .Take(pageSize)
                    .Select(x => new UnitPriceDto
                    {
                        Id = x.Id,
                        Name = x.Name,
                        Price = x.Price
                    }).ToListAsync(cancellationToken);

               PaginationMaker pagination = new PaginationMaker
                {
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalRecords = totalRecords
                };




                if (query == null)
                {
                    _logger.LogWarning("No unit prices found.");
                    return Response<List<UnitPriceDto>>.Fail(404, "No unit prices found.");
                }
                else { 
                    _logger.LogInformation("Unit prices retrieved successfully by {userıd}.",userId);
                    return Response<List<UnitPriceDto>>.Success(200, Units,pagination);

                }
            }
            catch (Exception ex)
            {

                _logger.LogError(ex, "An error occurred while retrieving unit prices.");
                return Response<List<UnitPriceDto>>.Fail(500, "An error occurred while processing your request.");
            }
            

        }
    }
}
