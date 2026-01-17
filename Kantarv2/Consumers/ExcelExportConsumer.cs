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
        private readonly IS3Service _s3Service;

        public ExcelExportConsumer(
            IServiceScopeFactory scopeFactory,
            IHubContext<ExcelExportHub> hubContext,
            ILogger<ExcelExportConsumer> logger,
            IS3Service s3Service)
        {
            _scopeFactory = scopeFactory;
            _hubContext = hubContext;
            _logger = logger;
            _s3Service = s3Service;
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

                // Excel dosyasını S3'e yükle ve pre-signed URL al (30 dakika geçerli)
                var (s3Key, downloadUrl) = await UploadToS3Async(exportResult, message);

                _logger.LogInformation(
                    "Excel export completed and uploaded to S3. CorrelationId={CorrelationId}, FileName={FileName}, S3Key={S3Key}",
                    message.CorrelationId,
                    exportResult.FileName,
                    s3Key);

                // SignalR ile kullanıcıya bildirim gönder
                await NotifyUserAsync(message, exportResult, s3Key, downloadUrl, isSuccess: true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Excel export failed. CorrelationId={CorrelationId}, Error={Error}",
                    message.CorrelationId,
                    ex.Message);

                // Hata durumunda da kullanıcıya bildirim gönder
                await NotifyUserAsync(message, null, null, null, isSuccess: false, errorMessage: ex.Message);

                throw;
            }
        }

        private async Task NotifyUserAsync(
            ExcelExportMessage message,
            ExportExcelDto? exportResult,
            string? s3Key,
            string? downloadUrl,
            bool isSuccess,
            string? errorMessage = null)
        {
            var notification = new ExcelExportNotification
            {
                CorrelationId = message.CorrelationId,
                FileName = exportResult?.FileName ?? string.Empty,
                DownloadUrl = downloadUrl ?? string.Empty,
                S3Key = s3Key ?? string.Empty,
                IsSuccess = isSuccess,
                ErrorMessage = errorMessage,
                CompletedAt = DateTime.UtcNow,
                ExpiresAt = isSuccess ? DateTime.UtcNow.AddMinutes(30) : null
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

        private async Task<(string S3Key, string DownloadUrl)> UploadToS3Async(ExportExcelDto exportResult, ExcelExportMessage message)
        {
            // S3 key: excel/userId/correlationId_filename.xlsx
            var s3Key = $"excel/{message.UserId}/{message.CorrelationId}_{exportResult.FileName}";

            // S3'e yükle
            await _s3Service.UploadFileAsync(exportResult.Content, s3Key);

            // 30 dakikalık pre-signed URL oluştur
            var downloadUrl = await _s3Service.GetPreSignedUrlAsync(s3Key, expirationMinutes: 30);

            _logger.LogInformation("Excel file uploaded to S3. Key={S3Key}, ExpiresIn=30min", s3Key);

            return (s3Key, downloadUrl);
        }
    }
}
