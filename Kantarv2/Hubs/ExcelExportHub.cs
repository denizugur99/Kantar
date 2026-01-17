using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Kantarv2.Hubs
{
    [Authorize]
    public class ExcelExportHub : Hub
    {
        private readonly ILogger<ExcelExportHub> _logger;

        public ExcelExportHub(ILogger<ExcelExportHub> logger)
        {
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, userId);
                _logger.LogInformation("User {UserId} connected to ExcelExportHub", userId);
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, userId);
                _logger.LogInformation("User {UserId} disconnected from ExcelExportHub", userId);
            }

            await base.OnDisconnectedAsync(exception);
        }
    }

    public record ExcelExportNotification
    {
        public Guid CorrelationId { get; init; }
        public string FileName { get; init; } = string.Empty;
        public string DownloadUrl { get; init; } = string.Empty;
        public string S3Key { get; init; } = string.Empty;
        public bool IsSuccess { get; init; }
        public string? ErrorMessage { get; init; }
        public DateTime CompletedAt { get; init; } = DateTime.UtcNow;
        public DateTime? ExpiresAt { get; init; }
    }
}
