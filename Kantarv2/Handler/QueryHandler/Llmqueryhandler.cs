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
    public class Llmqueryhandler : IRequestHandler<AskLlmQuery, Response<string>>
    {
        private readonly IDistributedCache _cache;
        private readonly ILogger<Llmqueryhandler> _logger;
        private readonly KantarDbContext _context;
        private readonly ILlmService _llmService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IWebHostEnvironment _environment;

        public Llmqueryhandler(
            IHttpContextAccessor httpContextAccessor,
            IDistributedCache cache,
            ILogger<Llmqueryhandler> logger,
            KantarDbContext context,
            ILlmService llmService,
            IWebHostEnvironment environment)
        {
            _httpContextAccessor = httpContextAccessor;
            _cache = cache;
            _logger = logger;
            _context = context;
            _llmService = llmService;
            _environment = environment;
        }

        public async Task<Response<string>> Handle(AskLlmQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? "Bilinmeyen Kullanici";

                string cleanPrompt = request.Prompt.Trim().ToLower();
                string cacheKey = $"Analysis:Llm:{ComputeHash(cleanPrompt)}";
                string? cachedResponse = await _cache.GetStringAsync(cacheKey, cancellationToken);

                if (!string.IsNullOrEmpty(cachedResponse))
                {
                    _logger.LogInformation("Onbellekten {userid} tarafindan LLM yaniti alindi.", userId);
                    return Response<string>.Success(200, cachedResponse);
                }

                // Urun detaylari
                var products = await _context.Products
                    .Include(p => p.UnitPrice)
                    .Where(p => !p.IsDeleted)
                    .Select(p => new ListProductDto()
                    {
                        Id = p.Id,
                        Name = p.UnitPrice.Name,
                        Status = p.Status,
                        Kilogram = p.Weight.ToString() + "kg",
                        Price = p.Price,
                        TotalPrice = p.TotalPrice,
                        CreatedDate = p.CreatedDate
                    }).ToListAsync(cancellationToken);

                // Fiyat ozeti
                var priceSummary = await _context.Products
                    .Include(x => x.UnitPrice)
                    .Where(p => !p.IsDeleted)
                    .GroupBy(p => p.UnitPrice)
                    .Select(g => new ListProductByUnitDto()
                    {
                        Name = g.Key.Name,
                        TotalWeight = g.Sum(s => s.Weight).ToString() + " kg",
                        TotalPrice = g.Sum(p => p.TotalPrice).ToString() + " TL"
                    }).ToListAsync(cancellationToken);

                var productsJson = System.Text.Json.JsonSerializer.Serialize(products);
                var priceSummaryJson = System.Text.Json.JsonSerializer.Serialize(priceSummary);

                // System prompt'u MD dosyasindan oku
                var systemPrompt = await GetSystemPromptAsync();

                var chatMessages = new List<object>
                {
                    new {
                        role = "system",
                        content = systemPrompt
                    },
                    new {
                        role = "user",
                        content = $@"Soru: {request.Prompt}

## Products (Urun Detaylari)
{productsJson}

## PriceSummary (Fiyat Ozeti)
{priceSummaryJson}"
                    }
                };

                var llmResponse = await _llmService.GenerateResponseAsync(chatMessages);

                var cacheOptions = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(4)
                };
                await _cache.SetStringAsync(cacheKey, llmResponse, cacheOptions, cancellationToken);
                _logger.LogInformation("LLM yaniti {userid} tarafindan basariyla alindi.", userId);
                return Response<string>.Success(200, llmResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError("LLM yaniti alinirken bir hata olustu: {ex}", ex);
                return Response<string>.Fail(500, "LLM yaniti alinirken bir hata olustu.");
            }
        }

        private async Task<string> GetSystemPromptAsync()
        {
            var promptPath = Path.Combine(_environment.ContentRootPath, "Prompts", "LlmSystemPrompt.md");

            if (File.Exists(promptPath))
            {
                return await File.ReadAllTextAsync(promptPath);
            }

            // Fallback prompt
            return @"Sen bir liman yonetim asistanisin. Urun verilerini analiz edersin.
Statu 1: Limandan Cikti, Statu 2: Limanda demektir.
Yanitlarini her zaman Turkce ve Markdown formatinda ver.
Soruya gore uygun veri setini kullan: Products (detayli urun bilgisi) veya PriceSummary (ozet bilgi).";
        }

        private string ComputeHash(string input)
        {
            using var md5 = System.Security.Cryptography.MD5.Create();
            var bytes = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(bytes);
        }
    }
}
