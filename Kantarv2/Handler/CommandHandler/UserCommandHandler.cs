using Kantarv2.Command.User;
using Kantarv2.DAL;
using Kantarv2.Dtos;
using Kantarv2.Entities;
using Kantarv2.Messages;
using Kantarv2.Services;
using MassTransit;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Kantarv2.Handler.CommandHandler
{
    public class UserCommandHandler : IRequestHandler<LoginCommand, Dtos.Response<TokenDto>>,
        IRequestHandler<RegisterCommand, Dtos.Response<NoContent>>,
        IRequestHandler<RefreshTokenCommand, Dtos.Response<TokenDto>>,
        IRequestHandler<DeleteUser, Dtos.Response<NoContent>>,
        IRequestHandler<LogoutCommand, Dtos.Response<NoContent>>,
        IRequestHandler<ForgotPasswordCommand, Dtos.Response<NoContent>>,
        IRequestHandler<ResetPasswordCommand, Dtos.Response<NoContent>>
    {
        private readonly ILogger<UserCommandHandler> _logger;
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly KantarDbContext _context;
        private readonly ITokenServiceInterface _tokenService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IPublishEndpoint _publishEndpoint;

        public UserCommandHandler(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            KantarDbContext context,
            ITokenServiceInterface tokenService,
            IHttpContextAccessor httpContextAccessor,
            ILogger<UserCommandHandler> logger,
            IPublishEndpoint publishEndpoint)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _tokenService = tokenService;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
            _publishEndpoint = publishEndpoint;
        }


        public async Task<Dtos.Response<TokenDto>> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // Find user by username (UserName property from IdentityUser)
                var user = await _userManager.Users
                    .Where(x => !x.IsDeleted && x.UserName.ToLower() == request.Username.Trim().ToLower())
                    .FirstOrDefaultAsync(cancellationToken);

                if (user == null)
                {
                    _logger.LogWarning("Kullanıcı bulunamadı:{Username}", request.Username);
                    return Dtos.Response<TokenDto>.Fail(500, "kullanıcı bulunamadı veya parola hatalı");
                }

                // Use SignInManager to check password
                var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: false);

                if (!result.Succeeded)
                {
                    _logger.LogWarning("Parola hatalı:{Username}", request.Username);
                    return Dtos.Response<TokenDto>.Fail(500, "kullanıcı bulunamadı veya parola hatalı");
                }

                var token = await _tokenService.CreateTokens(user);
                _logger.LogInformation("Kullanıcı başarıyla giriş yaptı:{Username}", user.UserName);
                return Dtos.Response<TokenDto>.Success(200, token);
            }
            catch (Exception ex)
            {
                _logger.LogError("Kullanıcı giriş yaparken hata oluştu:{Error}", ex.Message);
                return Dtos.Response<TokenDto>.Fail(500, "beklenmedik bir hata oluştu " + ex);
            }
        }

        public async Task<Dtos.Response<NoContent>> Handle(RegisterCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // Check if user exists by username
                var userExists = await _userManager.FindByNameAsync(request.Username.Trim());
                if (userExists != null)
                {
                    _logger.LogWarning("Kullanıcı zaten mevcut:{Username}", request.Username);
                    return Dtos.Response<NoContent>.Fail(500, "kullanıcı zaten mevcut");
                }

                var user = new User
                {
                    UserName = request.Username.Trim(),
                    Email = request.Email.Trim(),
                    IsDeleted = false
                };

                // Create user with UserManager (handles password hashing automatically)
                var result = await _userManager.CreateAsync(user, request.Password.Trim());

                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    _logger.LogError("Kullanıcı oluşturulurken hata: {Errors}", errors);
                    return Dtos.Response<NoContent>.Fail(500, "kullanıcı oluşturulamadı: " + errors);
                }

                // Assign role using UserManager
                var roleResult = await _userManager.AddToRoleAsync(user, request.Role.Trim());
                if (!roleResult.Succeeded)
                {
                    _logger.LogWarning("Rol atanamadı:{Username}, Role:{Role}", request.Username, request.Role);
                    // Note: User is created but role assignment failed
                }

                _logger.LogInformation("Kullanıcı başarıyla kaydedildi:{Username}", request.Username);
                return Dtos.Response<NoContent>.Success(200);
            }
            catch (Exception ex)
            {
                _logger.LogError("Kullanıcı kaydederken hata oluştu:{Error}", ex.Message);
                return Dtos.Response<NoContent>.Fail(500, "beklenmedik bir hata oluştu " + ex);
            }
        }

        public async Task<Dtos.Response<TokenDto>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // Find user by ID using UserManager
                var user = await _userManager.Users
                    .Where(x => !x.IsDeleted && x.Id == request.UserId)
                    .FirstOrDefaultAsync(cancellationToken);

                if (user == null)
                {
                    _logger.LogWarning("Kullanıcı bulunamadı:{UserId}", request.UserId);
                    return Dtos.Response<TokenDto>.Fail(500, "kullanıcı bulunamadı");
                }

                var token = await _tokenService.RefreshTokenAsync(user.Id, user.RefreshToken, cancellationToken);

                if (token == null)
                {
                    _logger.LogWarning("Token yenilenemedi:{UserId}", request.UserId);
                    return Dtos.Response<TokenDto>.Fail(500, "token geçersiz veya süresi dolmuş");
                }

                _logger.LogInformation("Token başarıyla yenilendi:{UserId}", request.UserId);
                return Dtos.Response<TokenDto>.Success(200, token);
            }
            catch (Exception ex)
            {
                _logger.LogError("Token yenilenirken hata oluştu:{Error}", ex.Message);
                return Dtos.Response<TokenDto>.Fail(500, "beklenmedik bir hata oluştu " + ex);
            }
        }

        public async Task<Dtos.Response<NoContent>> Handle(DeleteUser request, CancellationToken cancellationToken)
        {
            try
            {
                // Find user by ID
                var user = await _userManager.Users
                    .Where(x => !x.IsDeleted && x.Id == request.Id)
                    .FirstOrDefaultAsync(cancellationToken);

                if (user == null)
                {
                    _logger.LogWarning("Kullanıcı bulunamadı:{UserId}", request.Id);
                    return Dtos.Response<NoContent>.Fail(500, "kullanıcı bulunamadı");
                }

                // Soft delete by setting IsDeleted flag
                user.IsDeleted = true;
                var result = await _userManager.UpdateAsync(user);

                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    _logger.LogError("Kullanıcı silinirken hata:{Errors}", errors);
                    return Dtos.Response<NoContent>.Fail(500, "kullanıcı silinemedi: " + errors);
                }

                _logger.LogInformation("Kullanıcı başarıyla silindi:{UserId}", request.Id);
                return Dtos.Response<NoContent>.Success(200);
            }
            catch (Exception ex)
            {
                _logger.LogError("Kullanıcı silinirken hata oluştu:{Error}", ex.Message);
                return Dtos.Response<NoContent>.Fail(500, "beklenmedik bir hata oluştu " + ex);
            }
        }

        public async Task<Dtos.Response<NoContent>> Handle(LogoutCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // Get user ID from JWT token claims if not provided in request
                Guid userId = request.UserId;
                if (userId == Guid.Empty)
                {
                    var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                    if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out userId))
                    {
                        _logger.LogWarning("Kullanıcı kimliği JWT token'dan alınamadı");
                        return Dtos.Response<NoContent>.Fail(401, "Geçersiz token");
                    }
                }

                // Find user by ID
                var user = await _userManager.Users
                    .Where(x => !x.IsDeleted && x.Id == userId)
                    .FirstOrDefaultAsync(cancellationToken);

                if (user == null)
                {
                    _logger.LogWarning("Kullanıcı bulunamadı:{UserId}", userId);
                    return Dtos.Response<NoContent>.Fail(400, "kullanıcı bulunamadı");
                }

                // Refresh token kontrolü (opsiyonel - güvenlik için)
                if (!string.IsNullOrEmpty(request.RefreshToken) && user.RefreshToken != request.RefreshToken)
                {
                    _logger.LogWarning("Geçersiz refresh token:{UserId}", userId);
                    return Dtos.Response<NoContent>.Fail(400, "Geçersiz refresh token");
                }

                // CRITICAL: SecurityStamp'i güncelle - Tüm mevcut token'lar anında geçersiz olur!
                await _userManager.UpdateSecurityStampAsync(user);

                // Clear refresh token
                user.RefreshToken = null;
                user.RefreshTokenExpireDate = null;
                user.RefreshTokenVersion = 0; // Reset version
                await _userManager.UpdateAsync(user);

                _logger.LogInformation("Kullanıcı başarıyla çıkış yaptı. SecurityStamp güncellendi, tüm token'lar geçersiz:{UserId}", userId);
                return Dtos.Response<NoContent>.Success(204);
            }
            catch (Exception ex)
            {
                _logger.LogError("Kullanıcı çıkış yaparken hata oluştu:{Error}", ex.Message);
                return Dtos.Response<NoContent>.Fail(500, "beklenmedik bir hata oluştu " + ex);
            }
        }

        public async Task<Dtos.Response<NoContent>> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var user = await _userManager.Users
                    .Where(x => !x.IsDeleted && x.Email.ToLower() == request.Email.Trim().ToLower())
                    .FirstOrDefaultAsync(cancellationToken);

                if (user == null)
                {
                    // Guvenlik icin kullanici bulunamasa bile basarili donuyoruz
                    _logger.LogWarning("Sifre sifirlama istegi - kullanici bulunamadi: {Email}", request.Email);
                    return Dtos.Response<NoContent>.Success(200);
                }

                // Password reset token olustur
                var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);

                // MassTransit ile email gonder
                await _publishEndpoint.Publish(new PasswordResetMessage
                {
                    Email = user.Email!,
                    UserName = user.UserName!,
                    ResetToken = resetToken,
                    CreatedAt = DateTime.UtcNow
                }, cancellationToken);

                _logger.LogInformation("Sifre sifirlama emaili kuyruga eklendi: {Email}", user.Email);
                return Dtos.Response<NoContent>.Success(200);
            }
            catch (Exception ex)
            {
                _logger.LogError("Sifre sifirlama isteginde hata: {Error}", ex.Message);
                return Dtos.Response<NoContent>.Fail(500, "beklenmedik bir hata olustu " + ex);
            }
        }

        public async Task<Dtos.Response<NoContent>> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var user = await _userManager.Users
                    .Where(x => !x.IsDeleted && x.Email.ToLower() == request.Email.Trim().ToLower())
                    .FirstOrDefaultAsync(cancellationToken);

                if (user == null)
                {
                    _logger.LogWarning("Sifre sifirlama - kullanici bulunamadi: {Email}", request.Email);
                    return Dtos.Response<NoContent>.Fail(400, "Gecersiz istek");
                }

                var result = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);

                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    _logger.LogWarning("Sifre sifirlanamadi: {Email}, Hatalar: {Errors}", request.Email, errors);
                    return Dtos.Response<NoContent>.Fail(400, "Sifre sifirlanamadi: " + errors);
                }

                // SecurityStamp guncelle - eski token'lari gecersiz kil
                await _userManager.UpdateSecurityStampAsync(user);

                _logger.LogInformation("Sifre basariyla sifirlandi: {Email}", user.Email);
                return Dtos.Response<NoContent>.Success(200);
            }
            catch (Exception ex)
            {
                _logger.LogError("Sifre sifirlama hatasi: {Error}", ex.Message);
                return Dtos.Response<NoContent>.Fail(500, "beklenmedik bir hata olustu " + ex);
            }
        }
    }
}
