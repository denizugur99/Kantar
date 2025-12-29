using DocumentFormat.OpenXml.Bibliography;
using Kantarv2.DAL;
using Kantarv2.Dtos;
using Kantarv2.Queries.Llm;
using Kantarv2.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Security.Claims;

namespace Kantarv2.Handler.QueryHandler
{
    public class Llmqueryhandler : IRequestHandler<Productaskllmquery, Response<string>>
    {
        private readonly IDistributedCache _cache;
        private readonly ILogger<Llmqueryhandler> _logger;
        private readonly KantarDbContext _context;
        private readonly ILlmService _llmService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public Llmqueryhandler(IHttpContextAccessor httpContextAccessor, IDistributedCache cache,ILogger<Llmqueryhandler> logger, KantarDbContext context, ILlmService llmService)
        {
            _httpContextAccessor = httpContextAccessor;
            _cache = cache;
            _logger = logger;
            _context = context;
            _llmService = llmService;
        }

        public async Task<Response<string>> Handle(Productaskllmquery request, CancellationToken cancellationToken)
        {
            try
            {
                var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? "Bilinmeyen Kullanıcı";
                string cleanPrompt = request.Prompt.Trim().ToLower();
                string cacheKey = $"Analysis:{ComputeHash(cleanPrompt)}";
                string? cachedResponse = await _cache.GetStringAsync(cacheKey, cancellationToken);
                
                if (!string.IsNullOrEmpty(cachedResponse))
                {
                    _logger.LogInformation("Önbellekten {userid} tarafından LLM yanıtı alındı.",userId);
                    return Response<string>.Success(200, cachedResponse);
                }
                var products = await _context.Products.Include(p => p.UnitPrice).Where(p => !p.IsDeleted).Select(p => new ListProductDto()
            {
                    Id = p.Id,
                    Name = p.UnitPrice.Name,
                    Status = p.Status,
                    Kilogram = p.Weight.ToString() + "kg",
                    Price = p.Price,
                    TotalPrice = p.TotalPrice,
                    CreatedDate = p.CreatedDate
                }).ToListAsync(cancellationToken);
                var productsJson = System.Text.Json.JsonSerializer.Serialize(products);
                var finalPrompt = $@"
            Aşağıdaki limandaki ürün verilerini kullanarak şu isteği yerine getir: '{request.Prompt}'
            Statüsü bir olanla limandan çıkmış statüsü 2 olanlar ise şuan limandadır
            {productsJson}
            
            Lütfen yanıtı Türkçe ve Markdown formatında ver.";

                var llmResponse = await _llmService.GenerateResponseAsync(finalPrompt);

                var cacheOptions = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(4)
                };
                await _cache.SetStringAsync(cacheKey, llmResponse, cacheOptions, cancellationToken);
                _logger.LogInformation("LLM yanıtı {userid} tarafından başarıyla alındı.",userId);
                return Response<string>.Success(200, llmResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError("LLM yanıtı alınırken bir hata oluştu.");
                return Response<string>.Fail(500, "LLM yanıtı alınırken bir hata oluştu.");


            }
          
        }
        private string ComputeHash(string input)
        {
            using var md5 = System.Security.Cryptography.MD5.Create();
            var bytes = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(bytes);
        }
    }
}
