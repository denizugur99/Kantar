using Kantarv2.DAL;
using Kantarv2.Dtos;
using Kantarv2.Enums;
using Kantarv2.Hubs;
using Kantarv2.Messages;
using Kantarv2.Services;
using MassTransit;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Kantarv2.Consumers
{
    public class ExcelExportConsumer : IConsumer<ExcelExportMessage>
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IHubContext<ExcelExportHub> _hubContext;
        private readonly ILogger<ExcelExportConsumer> _logger;

        public ExcelExportConsumer(
            IServiceScopeFactory scopeFactory,
            IHubContext<ExcelExportHub> hubContext,
            ILogger<ExcelExportConsumer> logger)
        {
            _scopeFactory = scopeFactory;
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<ExcelExportMessage> context)
        {
            var message = context.Message;

            _logger.LogInformation(
                "Excel export started. CorrelationId={CorrelationId}, UserId={UserId}, ExportType={ExportType}",
                message.CorrelationId,
                message.UserId,
                message.ExportType);

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<KantarDbContext>();
                var excelService = scope.ServiceProvider.GetRequiredService<IExcelServiceInterface>();

                var exportResult = message.ExportType switch
                {
                    ExcelExportType.ProductList => await BuildProductListExcel(dbContext, excelService, message),
                    ExcelExportType.ProductByUnit => await BuildProductByUnitExcel(dbContext, excelService, message),
                    _ => throw new ArgumentException($"Unknown export type: {message.ExportType}")
                };

                // Excel dosyasını kaydet
                var filePath = await SaveExcelFile(exportResult, message);

                _logger.LogInformation(
                    "Excel export completed. CorrelationId={CorrelationId}, FileName={FileName}",
                    message.CorrelationId,
                    exportResult.FileName);

                // SignalR ile kullanıcıya bildirim gönder
                await NotifyUserAsync(message, exportResult, filePath, isSuccess: true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Excel export failed. CorrelationId={CorrelationId}, Error={Error}",
                    message.CorrelationId,
                    ex.Message);

                // Hata durumunda da kullanıcıya bildirim gönder
                await NotifyUserAsync(message, null, null, isSuccess: false, errorMessage: ex.Message);

                throw;
            }
        }

        private async Task NotifyUserAsync(
            ExcelExportMessage message,
            ExportExcelDto? exportResult,
            string? filePath,
            bool isSuccess,
            string? errorMessage = null)
        {
            var notification = new ExcelExportNotification
            {
                CorrelationId = message.CorrelationId,
                FileName = exportResult?.FileName ?? string.Empty,
                DownloadUrl = isSuccess ? $"/api/products/download/{message.CorrelationId}" : string.Empty,
                IsSuccess = isSuccess,
                ErrorMessage = errorMessage,
                CompletedAt = DateTime.UtcNow
            };

            // Kullanıcının group'una bildirim gönder (UserId bazlı group)
            await _hubContext.Clients
                .Group(message.UserId.ToString())
                .SendAsync("ExcelExportCompleted", notification);

            _logger.LogInformation(
                "SignalR notification sent to user {UserId}. CorrelationId={CorrelationId}, IsSuccess={IsSuccess}",
                message.UserId,
                message.CorrelationId,
                isSuccess);
        }

        private async Task<ExportExcelDto> BuildProductListExcel(
            KantarDbContext context,
            IExcelServiceInterface excelService,
            ExcelExportMessage message)
        {
            var filters = message.Filters;

            var query = context.Products
                .Include(x => x.UnitPrice)
                .Where(p => !p.IsDeleted);

            // SearchTerm filtresi
            if (!string.IsNullOrEmpty(filters.SearchTerm))
            {
                var searchTerm = filters.SearchTerm.Trim().ToLower();
                query = query.Where(p => p.UnitPrice.Name.Trim().ToLower().Contains(searchTerm));
            }

            // Tarih filtreleri
            if (filters.StartDate.HasValue)
            {
                var startDate = filters.StartDate.Value.Date;
                query = query.Where(q => q.CreatedDate >= startDate);
            }

            if (filters.EndDate.HasValue)
            {
                var endDate = filters.EndDate.Value.Date.AddDays(1);
                query = query.Where(q => q.CreatedDate <= endDate);
            }

            var records = await query
                .Select(q => new ListProductDto
                {
                    Id = q.Id,
                    Name = q.UnitPrice.Name,
                    Status = q.Status,
                    Kilogram = q.Weight.ToString() + " kilogram",
                    Price = q.Price,
                    TotalPrice = q.TotalPrice,
                    CreatedDate = q.CreatedDate.Date,
                })
                .OrderBy(x => x.CreatedDate)
                .ToListAsync();

            var fileName = BuildFileName("UrunListesi", filters);
            var fileContent = excelService.GenerateExcel(records, "Ürünlistelesi");

            return new ExportExcelDto
            {
                FileName = fileName,
                Content = fileContent
            };
        }

        private async Task<ExportExcelDto> BuildProductByUnitExcel(
            KantarDbContext context,
            IExcelServiceInterface excelService,
            ExcelExportMessage message)
        {
            var filters = message.Filters;

            var query = context.Products
                .Include(x => x.UnitPrice)
                .Where(p => !p.IsDeleted);

            // SearchTerm filtresi
            if (!string.IsNullOrEmpty(filters.SearchTerm))
            {
                var searchTerm = filters.SearchTerm.Trim().ToLower();
                query = query.Where(p => p.UnitPrice.Name.Trim().ToLower().Contains(searchTerm));
            }

            // Tarih filtreleri
            if (filters.StartDate.HasValue)
            {
                var startDate = filters.StartDate.Value.Date;
                query = query.Where(q => q.CreatedDate >= startDate);
            }

            if (filters.EndDate.HasValue)
            {
                var endDate = filters.EndDate.Value.Date.AddDays(1);
                query = query.Where(q => q.CreatedDate <= endDate);
            }

            var records = await query
                .GroupBy(x => x.UnitPrice)
                .Select(x => new ListProductByUnitDto
                {
                    Name = x.Key.Name,
                    TotalWeight = x.Sum(s => s.Weight).ToString() + " kg",
                    TotalPrice = x.Sum(p => p.TotalPrice).ToString() + " TL"
                })
                .ToListAsync();

            var fileName = BuildFileName("UrunBirimListesi", filters);
            var fileContent = excelService.GenerateExcel(records, "Ürünlistelesi");

            return new ExportExcelDto
            {
                FileName = fileName,
                Content = fileContent
            };
        }

        private static string BuildFileName(string prefix, ExcelExportFilters filters)
        {
            string dateRange = "Tum_Zamanlar";

            if (filters.StartDate.HasValue && filters.EndDate.HasValue)
            {
                dateRange = $"{filters.StartDate.Value:dd_MM_yyyy}_to_{filters.EndDate.Value:dd_MM_yyyy}";
            }
            else if (filters.StartDate.HasValue)
            {
                dateRange = $"{filters.StartDate.Value:dd_MM_yyyy}_Sonrasi";
            }

            return $"{prefix}_{dateRange}.xlsx";
        }

        private async Task<string> SaveExcelFile(ExportExcelDto exportResult, ExcelExportMessage message)
        {
            // Export klasörünü oluştur
            var exportPath = Path.Combine(Directory.GetCurrentDirectory(), "Exports", message.UserId.ToString());
            Directory.CreateDirectory(exportPath);

            var filePath = Path.Combine(exportPath, $"{message.CorrelationId}_{exportResult.FileName}");

            await File.WriteAllBytesAsync(filePath, exportResult.Content);

            _logger.LogInformation("Excel file saved to {FilePath}", filePath);

            return filePath;
        }
    }
}
