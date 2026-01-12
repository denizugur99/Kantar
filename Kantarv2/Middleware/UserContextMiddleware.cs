using Serilog.Context;
using System.Security.Claims;

namespace Kantarv2.Middleware
{
    /// <summary>
    /// Her HTTP isteğinde kullanıcı bilgilerini Serilog log context'ine ekler
    /// </summary>
    public class UserContextMiddleware
    {
        private readonly RequestDelegate _next;

        public UserContextMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Kullanıcı authenticate ise bilgilerini context'e ekle
            if (context.User?.Identity?.IsAuthenticated == true)
            {
                var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userName = context.User.FindFirst(ClaimTypes.Name)?.Value;
                var userEmail = context.User.FindFirst(ClaimTypes.Email)?.Value;
                var userRoles = string.Join(", ", context.User.FindAll(ClaimTypes.Role).Select(c => c.Value));

                // Serilog LogContext'e property'ler ekle
                using (LogContext.PushProperty("UserId", userId ?? "Anonymous"))
                using (LogContext.PushProperty("UserName", userName ?? "Anonymous"))
                using (LogContext.PushProperty("UserEmail", userEmail ?? "N/A"))
                using (LogContext.PushProperty("UserRoles", !string.IsNullOrEmpty(userRoles) ? userRoles : "None"))
                {
                    await _next(context);
                }
            }
            else
            {
                // Anonymous kullanıcı
                using (LogContext.PushProperty("UserId", "Anonymous"))
                using (LogContext.PushProperty("UserName", "Anonymous"))
                {
                    await _next(context);
                }
            }
        }
    }
}
