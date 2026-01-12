using Kantarv2.Command.User;
using Kantarv2.DAL;
using Kantarv2.Dtos;
using Kantarv2.Entities;
using Kantarv2.Services;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Kantarv2.Handler.CommandHandler
{
    public class UserCommandHandler : IRequestHandler<LoginCommand, Response<TokenDto>>,
        IRequestHandler<RegisterCommand, Response<NoContent>>,
        IRequestHandler<RefreshTokenCommand, Response<TokenDto>>,
        IRequestHandler<DeleteUser, Response<NoContent>>,
        IRequestHandler<LogoutCommand, Response<NoContent>>
    {
        private readonly ILogger<UserCommandHandler> _logger;
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly KantarDbContext _context;
        private readonly ITokenServiceInterface _tokenService;

        private readonly IHttpContextAccessor _httpContextAccessor;

        public UserCommandHandler(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            KantarDbContext context,
            ITokenServiceInterface tokenService,
            IHttpContextAccessor httpContextAccessor,
            ILogger<UserCommandHandler> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _tokenService = tokenService;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }


        public async Task<Response<TokenDto>> Handle(LoginCommand request, CancellationToken cancellationToken)
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
                    return Response<TokenDto>.Fail(500, "kullanıcı bulunamadı veya parola hatalı");
                }

                // Use SignInManager to check password
                var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: false);

                if (!result.Succeeded)
                {
                    _logger.LogWarning("Parola hatalı:{Username}", request.Username);
                    return Response<TokenDto>.Fail(500, "kullanıcı bulunamadı veya parola hatalı");
                }

                var token = await _tokenService.CreateTokens(user);
                _logger.LogInformation("Kullanıcı başarıyla giriş yaptı:{Username}", user.UserName);
                return Response<TokenDto>.Success(200, token);
            }
            catch (Exception ex)
            {
                _logger.LogError("Kullanıcı giriş yaparken hata oluştu:{Error}", ex.Message);
                return Response<TokenDto>.Fail(500, "beklenmedik bir hata oluştu " + ex);
            }
        }

        public async Task<Response<NoContent>> Handle(RegisterCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // Check if user exists by username
                var userExists = await _userManager.FindByNameAsync(request.Username.Trim());
                if (userExists != null)
                {
                    _logger.LogWarning("Kullanıcı zaten mevcut:{Username}", request.Username);
                    return Response<NoContent>.Fail(500, "kullanıcı zaten mevcut");
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
                    return Response<NoContent>.Fail(500, "kullanıcı oluşturulamadı: " + errors);
                }

                // Assign role using UserManager
                var roleResult = await _userManager.AddToRoleAsync(user, request.Role.Trim());
                if (!roleResult.Succeeded)
                {
                    _logger.LogWarning("Rol atanamadı:{Username}, Role:{Role}", request.Username, request.Role);
                    // Note: User is created but role assignment failed
                }

                _logger.LogInformation("Kullanıcı başarıyla kaydedildi:{Username}", request.Username);
                return Response<NoContent>.Success(200);
            }
            catch (Exception ex)
            {
                _logger.LogError("Kullanıcı kaydederken hata oluştu:{Error}", ex.Message);
                return Response<NoContent>.Fail(500, "beklenmedik bir hata oluştu " + ex);
            }
        }

        public async Task<Response<TokenDto>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
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
                    return Response<TokenDto>.Fail(500, "kullanıcı bulunamadı");
                }

                var token = await _tokenService.RefreshTokenAsync(user.Id, user.RefreshToken, cancellationToken);

                if (token == null)
                {
                    _logger.LogWarning("Token yenilenemedi:{UserId}", request.UserId);
                    return Response<TokenDto>.Fail(500, "token geçersiz veya süresi dolmuş");
                }

                _logger.LogInformation("Token başarıyla yenilendi:{UserId}", request.UserId);
                return Response<TokenDto>.Success(200, token);
            }
            catch (Exception ex)
            {
                _logger.LogError("Token yenilenirken hata oluştu:{Error}", ex.Message);
                return Response<TokenDto>.Fail(500, "beklenmedik bir hata oluştu " + ex);
            }
        }

        public async Task<Response<NoContent>> Handle(DeleteUser request, CancellationToken cancellationToken)
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
                    return Response<NoContent>.Fail(500, "kullanıcı bulunamadı");
                }

                // Soft delete by setting IsDeleted flag
                user.IsDeleted = true;
                var result = await _userManager.UpdateAsync(user);

                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    _logger.LogError("Kullanıcı silinirken hata:{Errors}", errors);
                    return Response<NoContent>.Fail(500, "kullanıcı silinemedi: " + errors);
                }

                _logger.LogInformation("Kullanıcı başarıyla silindi:{UserId}", request.Id);
                return Response<NoContent>.Success(200);
            }
            catch (Exception ex)
            {
                _logger.LogError("Kullanıcı silinirken hata oluştu:{Error}", ex.Message);
                return Response<NoContent>.Fail(500, "beklenmedik bir hata oluştu " + ex);
            }
        }

        public async Task<Response<NoContent>> Handle(LogoutCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // Get user ID from JWT token claims if not provided in request
                int userId = request.UserId;
                if (userId == 0)
                {
                    var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                    if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out userId))
                    {
                        _logger.LogWarning("Kullanıcı kimliği JWT token'dan alınamadı");
                        return Response<NoContent>.Fail(401, "Geçersiz token");
                    }
                }

                // Find user by ID and refresh token
                var user = await _userManager.Users
                    .Where(x => !x.IsDeleted && x.Id == userId && x.RefreshToken == request.RefreshToken)
                    .FirstOrDefaultAsync(cancellationToken);

                if (user == null)
                {
                    _logger.LogWarning("Kullanıcı bulunamadı:{UserId}", userId);
                    return Response<NoContent>.Fail(400, "kullanıcı bulunamadı veya refresh token geçersiz");
                }

                // Clear refresh token (JWT is stateless, so we only invalidate the refresh token)
                user.RefreshToken = null;
                user.RefreshTokenExpireDate = null;
                var result = await _userManager.UpdateAsync(user);

                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    _logger.LogError("Çıkış yaparken hata:{Errors}", errors);
                    return Response<NoContent>.Fail(500, "çıkış yapılamadı: " + errors);
                }

                // Note: No need to call SignOutAsync() for JWT authentication
                // JWT tokens are stateless and managed by the client
                // We only invalidate the refresh token to prevent token renewal

                _logger.LogInformation("Kullanıcı başarıyla çıkış yaptı:{UserId}", request.UserId);
                return Response<NoContent>.Success(204);
            }
            catch (Exception ex)
            {
                _logger.LogError("Kullanıcı çıkış yaparken hata oluştu:{Error}", ex.Message);
                return Response<NoContent>.Fail(500, "beklenmedik bir hata oluştu " + ex);
            }
        }
    }
}
