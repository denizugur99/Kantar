using Kantarv2.Command.Product;
using Kantarv2.Queries.Products;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kantarv2.Controllers
{
    /// <summary>
    /// Ürün yönetimi işlemleri (ekleme, silme, listeleme, sevkiyat, excel export)
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : BaseController
    {
        private readonly IMediator _mediator;
        public ProductsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Yeni ürün ekler
        /// </summary>
        /// <param name="command">Ürün bilgileri (weight: ağırlık kg, unitId: birim fiyat ID, price: manuel fiyat opsiyonel)</param>
        /// <returns>Eklenen ürün bilgileri ve toplam fiyat</returns>
        /// <response code="201">Ürün başarıyla eklendi</response>
        /// <response code="400">Geçersiz birim ID veya veri</response>
        [Authorize(Roles = "SuperAdmin,Admin")]
        [HttpPost("add")]
        public async Task<IActionResult> AddProduct(AddProduct command)
        {
            var result = await _mediator.Send(command);
            return CreateActionResultInstance(result);
        }

        /// <summary>
        /// Ürünü siler (soft delete)
        /// </summary>
        /// <param name="command">Silinecek ürün ID</param>
        /// <returns>Silme sonucu</returns>
        /// <response code="200">Ürün başarıyla silindi</response>
        /// <response code="404">Ürün bulunamadı</response>
        [Authorize(Roles = "SuperAdmin,Admin")]
        [HttpDelete("delete")]
        public async Task<IActionResult> DeleteProduct(DeleteProduct command)
        {
            var result = await _mediator.Send(command);
            return CreateActionResultInstance(result);
        }

        /// <summary>
        /// Ürünü sevkiyat durumuna alır
        /// </summary>
        /// <param name="command">Sevk bilgileri (weight, unitId, price opsiyonel)</param>
        /// <returns>Sevk edilen ürün bilgileri</returns>
        /// <response code="200">Ürün başarıyla sevk edildi</response>
        [Authorize(Roles = "SuperAdmin,Admin")]
        [HttpPost("ship")]
        public async Task<IActionResult> ShipProduct(ShipProduct command)
        {
            var result = await _mediator.Send(command);
            return CreateActionResultInstance(result);
        }

        /// <summary>
        /// Tüm ürünleri sayfalı ve filtrelenmiş olarak listeler
        /// </summary>
        /// <param name="pagesize">Sayfa başına kayıt sayısı</param>
        /// <param name="pagenumber">Sayfa numarası</param>
        /// <param name="startdate">Başlangıç tarihi filtresi (opsiyonel)</param>
        /// <param name="enddate">Bitiş tarihi filtresi (opsiyonel)</param>
        /// <param name="search">Ürün adı araması (opsiyonel)</param>
        /// <returns>Ürün listesi ve sayfalama bilgisi</returns>
        /// <response code="200">Liste başarıyla döndü</response>
        /// <response code="400">Ürün bulunamadı</response>
        [Authorize(Roles = "SuperAdmin,Admin,User")]
        [HttpGet("all/{pagesize?}/{pagenumber?}")]
        public async Task<IActionResult> AllProducts(int pagesize, int pagenumber, [FromQuery] DateTime? startdate, DateTime? enddate, string? search)
        {
            var result = await _mediator.Send(new ListProduct()
            {
                PageNumber = pagenumber,
                PageSize = pagesize,
                SearchTerm = search,
                StartDate = startdate,
                EndDate = enddate
            });
            return CreateActionResultInstance(result);
        }

        /// <summary>
        /// Ürünleri birim bazında gruplandırılmış özet olarak listeler
        /// </summary>
        /// <param name="pagesize">Sayfa başına kayıt sayısı</param>
        /// <param name="pagenumber">Sayfa numarası</param>
        /// <param name="startdate">Başlangıç tarihi filtresi (opsiyonel)</param>
        /// <param name="enddate">Bitiş tarihi filtresi (opsiyonel)</param>
        /// <param name="id">Belirli birim ID filtresi (opsiyonel)</param>
        /// <returns>Birim bazlı toplam ağırlık ve fiyat özeti</returns>
        /// <response code="200">Özet başarıyla döndü</response>
        [Authorize(Roles = "SuperAdmin,Admin,User")]
        [HttpGet("summary/{pagesize?}/{pagenumber?}")]
        public async Task<IActionResult> GetSum(int pagesize, int pagenumber, [FromQuery] DateTime? startdate, DateTime? enddate, Guid? id)
        {
            var result = await _mediator.Send(new ListProductByUnit()
            {
                PageNumber = pagenumber,
                Pagesize = pagesize,
                Id = id,
                StartTime = startdate,
                EndTime = enddate
            });
            return CreateActionResultInstance(result);
        }

        /// <summary>
        /// Excel export isteği oluşturur (asenkron). İşlem tamamlandığında SignalR ile bildirim gönderilir.
        /// </summary>
        /// <param name="request">Export tipi (1=ProductList, 2=ProductByUnit), tarih aralığı ve arama filtresi</param>
        /// <returns>CorrelationId - export durumunu takip etmek için kullanılır</returns>
        /// <response code="202">Export isteği kuyruğa alındı</response>
        /// <response code="401">Kimlik doğrulama gerekli</response>
        /// <remarks>
        /// Export tipleri:
        /// - 1 (ProductList): Detaylı ürün listesi
        /// - 2 (ProductByUnit): Birim bazlı özet raporu
        ///
        /// SignalR Hub: /hubs/excel-export adresinden "ExcelExportCompleted" event'ini dinleyin
        /// </remarks>
        [HttpPost("export")]
        public async Task<IActionResult> RequestExcelExport([FromBody] RequestExcelExport request)
        {
            var result = await _mediator.Send(request);
            return CreateActionResultInstance(result);
        }

        /// <summary>
        /// Hazırlanan Excel dosyasını indirir
        /// </summary>
        /// <param name="correlationId">Export isteğinden dönen correlation ID</param>
        /// <returns>Excel dosyası (.xlsx)</returns>
        /// <response code="200">Dosya başarıyla döndü</response>
        /// <response code="404">Dosya bulunamadı veya süresi dolmuş (30 dk)</response>
        /// <response code="401">Kimlik doğrulama gerekli</response>
        /// <remarks>
        /// Dosyalar S3'te 30 dakika saklanır, sonra otomatik silinir.
        /// Alternatif olarak SignalR bildirimindeki pre-signed URL doğrudan kullanılabilir.
        /// </remarks>
        [Authorize(Roles = "SuperAdmin,Admin,User")]
        [HttpGet("download/{correlationId}")]
        public async Task<IActionResult> DownloadExcel(Guid correlationId)
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            var result = await _mediator.Send(new DownloadExcelQuery
            {
                CorrelationId = correlationId,
                UserId = userId
            });

            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result.Errors);
            }

            return File(result.Data.Content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", result.Data.FileName);
        }
    }
}
