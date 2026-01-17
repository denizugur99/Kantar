using Kantarv2.Command.User;
using Kantarv2.Queries;
using Kantarv2.Queries.User;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Runtime.CompilerServices;

namespace Kantarv2.Controllers
{
    /// <summary>
    /// Kullanıcı yönetimi ve kimlik doğrulama işlemleri
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : BaseController
    {
        private readonly IMediator _mediator;
        public UserController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Kullanıcı girişi yapar ve JWT token döner
        /// </summary>
        /// <param name="command">Kullanıcı adı ve şifre</param>
        /// <returns>Access token ve refresh token</returns>
        /// <response code="200">Giriş başarılı</response>
        /// <response code="401">Kullanıcı adı veya şifre hatalı</response>
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginCommand command)
        {
            var result = await _mediator.Send(command);
            return CreateActionResultInstance(result);
        }

        /// <summary>
        /// Yeni kullanıcı kaydı oluşturur
        /// </summary>
        /// <param name="command">Kullanıcı bilgileri (username, password, email, role)</param>
        /// <returns>Kayıt sonucu</returns>
        /// <response code="201">Kullanıcı başarıyla oluşturuldu</response>
        /// <response code="400">Geçersiz veri veya kullanıcı zaten mevcut</response>
        [Authorize(Roles = "SuperAdmin")]
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterCommand command)
        {
            var result = await _mediator.Send(command);
            return CreateActionResultInstance(result);
        }

        /// <summary>
        /// Süresi dolan access token'ı yeniler
        /// </summary>
        /// <param name="command">UserId ve RefreshToken</param>
        /// <returns>Yeni access token ve refresh token</returns>
        /// <response code="200">Token başarıyla yenilendi</response>
        /// <response code="401">Refresh token geçersiz veya süresi dolmuş</response>
        [HttpPost("refreshtoken")]
        public async Task<IActionResult> RefreshToken(RefreshTokenCommand command)
        {
            var result = await _mediator.Send(command);
            return CreateActionResultInstance(result);
        }

        /// <summary>
        /// Tüm kullanıcıları sayfalı olarak listeler
        /// </summary>
        /// <param name="pagenumber">Sayfa numarası</param>
        /// <param name="pagesize">Sayfa başına kayıt sayısı</param>
        /// <param name="search">Arama terimi (kullanıcı adı veya email)</param>
        /// <returns>Kullanıcı listesi ve sayfalama bilgisi</returns>
        /// <response code="200">Liste başarıyla döndü</response>
        /// <response code="403">Yetki yok (sadece SuperAdmin)</response>
        [Authorize(Roles = "SuperAdmin")]
        [HttpGet("all/{pagenumber?}/{pagesize?}")]
        public async Task<IActionResult> GetAll(int pagenumber, int pagesize, [FromQuery] string? search)
        {
            var result = await _mediator.Send(new ListAllQuery()
            {
                PageNumber = pagenumber,
                PageSize = pagesize,
                Search = search
            });
            return CreateActionResultInstance(result);
        }

        /// <summary>
        /// ID ile kullanıcı detayını getirir
        /// </summary>
        /// <param name="id">Kullanıcı ID (GUID)</param>
        /// <returns>Kullanıcı detay bilgileri</returns>
        /// <response code="200">Kullanıcı bulundu</response>
        /// <response code="404">Kullanıcı bulunamadı</response>
        [Authorize(Roles = "SuperAdmin")]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _mediator.Send(new FindUserById { Id = id });
            return CreateActionResultInstance(result);
        }

        /// <summary>
        /// Kullanıcıyı siler (soft delete)
        /// </summary>
        /// <param name="id">Silinecek kullanıcı ID</param>
        /// <returns>Silme sonucu</returns>
        /// <response code="200">Kullanıcı başarıyla silindi</response>
        /// <response code="404">Kullanıcı bulunamadı</response>
        [Authorize(Roles = "SuperAdmin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(Guid id)
        {
            var result = await _mediator.Send(new DeleteUser { Id = id });
            return CreateActionResultInstance(result);
        }

        /// <summary>
        /// Kullanıcı oturumunu sonlandırır ve token'ları geçersiz kılar
        /// </summary>
        /// <param name="command">RefreshToken (UserId opsiyonel, JWT'den alınabilir)</param>
        /// <returns>Çıkış sonucu</returns>
        /// <response code="200">Başarıyla çıkış yapıldı</response>
        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout(LogoutCommand command)
        {
            var result = await _mediator.Send(command);
            return CreateActionResultInstance(result);
        }

        /// <summary>
        /// Şifre sıfırlama e-postası gönderir
        /// </summary>
        /// <param name="command">Kullanıcı e-posta adresi</param>
        /// <returns>İşlem sonucu</returns>
        /// <response code="200">E-posta gönderildi (kullanıcı varsa)</response>
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordCommand command)
        {
            var result = await _mediator.Send(command);
            return CreateActionResultInstance(result);
        }

        /// <summary>
        /// E-posta ile gelen token kullanarak şifre sıfırlar
        /// </summary>
        /// <param name="command">Email, token ve yeni şifre</param>
        /// <returns>Şifre sıfırlama sonucu</returns>
        /// <response code="200">Şifre başarıyla değiştirildi</response>
        /// <response code="400">Token geçersiz veya süresi dolmuş</response>
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(ResetPasswordCommand command)
        {
            var result = await _mediator.Send(command);
            return CreateActionResultInstance(result);
        }
    }
}
