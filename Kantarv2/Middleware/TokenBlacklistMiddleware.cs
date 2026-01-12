using Kantarv2.Services;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System.Threading.Tasks;

namespace Kantarv2.Middleware
{
    public class TokenBlacklistMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<TokenBlacklistMiddleware> _logger;

        public TokenBlacklistMiddleware(RequestDelegate next, ILogger<TokenBlacklistMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, ITokenBlacklistService blacklistService)
        {
            // Authorization header'dan token'ı al
            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();

            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                var token = authHeader.Substring("Bearer ".Length).Trim();

                // Token blacklist'te mi kontrol et
                if (await blacklistService.IsTokenBlacklistedAsync(token))
                {
                    _logger.LogWarning("Blacklist'teki token ile erişim denemesi yapıldı");

                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsJsonAsync(new
                    {
                        statusCode = 401,
                        isSuccessful = false,
                        errors = new[] { "Token geçersiz. Lütfen tekrar giriş yapın." }
                    });
                    return;
                }
            }

            await _next(context);
        }
    }
}
