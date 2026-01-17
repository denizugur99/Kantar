using Kantarv2.Command.UnitPrice;
using Kantarv2.Queries.UnitPrice;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Kantarv2.Controllers
{
    /// <summary>
    /// Birim fiyat yönetimi (ürün kategorileri ve kg başına fiyatlar)
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class UnitPriceController : BaseController
    {
        private readonly IMediator _mediator;

        public UnitPriceController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Yeni birim fiyat tanımı oluşturur
        /// </summary>
        /// <param name="command">Birim adı ve kg başına fiyat</param>
        /// <returns>Oluşturulan birim fiyat bilgileri</returns>
        /// <response code="201">Birim fiyat başarıyla oluşturuldu</response>
        /// <response code="400">Geçersiz veri veya aynı isimde birim mevcut</response>
        /// <example>
        /// Request: { "name": "Elma", "price": 25.50 }
        /// </example>
        [Authorize(Roles = "SuperAdmin,Admin")]
        [HttpPost("add")]
        public async Task<IActionResult> AddUnitPrice([FromBody] AddUnitPrice command)
        {
            var result = await _mediator.Send(command);
            return CreateActionResultInstance(result);
        }

        /// <summary>
        /// Tüm birim fiyatları sayfalı olarak listeler
        /// </summary>
        /// <param name="pagesize">Sayfa başına kayıt sayısı</param>
        /// <param name="pagenumber">Sayfa numarası</param>
        /// <param name="search">Birim adı araması (opsiyonel)</param>
        /// <returns>Birim fiyat listesi ve sayfalama bilgisi</returns>
        /// <response code="200">Liste başarıyla döndü</response>
        [Authorize(Roles = "SuperAdmin,Admin,User")]
        [HttpGet("all/{pagesize?}/{pagenumber?}")]
        public async Task<IActionResult> GetAllUnitPrices(int pagesize, int pagenumber, [FromQuery] string? search)
        {
            var result = await _mediator.Send(new GetAllUnitPrice()
            {
                PageSize = pagesize,
                PageNumber = pagenumber,
                SearchTerm = search
            });
            return CreateActionResultInstance(result);
        }

        /// <summary>
        /// Mevcut birim fiyatını günceller
        /// </summary>
        /// <param name="command">Birim ID ve yeni fiyat</param>
        /// <returns>Güncellenmiş birim fiyat bilgileri</returns>
        /// <response code="200">Birim fiyat başarıyla güncellendi</response>
        /// <response code="404">Birim bulunamadı</response>
        /// <example>
        /// Request: { "id": "guid-here", "price": 30.00 }
        /// </example>
        [Authorize(Roles = "SuperAdmin,Admin")]
        [HttpPut("update")]
        public async Task<IActionResult> UpdateUnitPrice([FromBody] UpdateUnitPrice command)
        {
            var result = await _mediator.Send(command);
            return CreateActionResultInstance(result);
        }

        /// <summary>
        /// Birim fiyatı siler
        /// </summary>
        /// <param name="command">Silinecek birim ID</param>
        /// <returns>Silme sonucu</returns>
        /// <response code="200">Birim fiyat başarıyla silindi</response>
        /// <response code="404">Birim bulunamadı</response>
        /// <response code="400">Bu birime bağlı ürünler var, silinemez</response>
        [Authorize(Roles = "SuperAdmin,Admin")]
        [HttpDelete("delete")]
        public async Task<IActionResult> DeleteUnitPrice([FromBody] DeleteUnitPrice command)
        {
            var result = await _mediator.Send(command);
            return CreateActionResultInstance(result);
        }
    }
}
