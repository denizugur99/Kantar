using Kantarv2.Command.User;
using Kantarv2.DAL;
using Kantarv2.Dtos;
using Kantarv2.Entities;
using Kantarv2.Services;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Kantarv2.Handler.CommandHandler
{
    public class UserCommandHandler : IRequestHandler<LoginCommand, Response<TokenDto>>,
        IRequestHandler<RegisterCommand, Response<NoContent>>,
        IRequestHandler<RefreshTokenCommand, Response<TokenDto>>,
        IRequestHandler<DeleteUser, Response<NoContent>>,
        IRequestHandler<LogoutCommand, Response<NoContent>>
    {
        private readonly ILogger<UserCommandHandler> _logger;
        private readonly KantarDbContext _context;
        private readonly ITokenServiceInterface _tokenService;
        public UserCommandHandler(KantarDbContext context,ITokenServiceInterface tokenService, ILogger<UserCommandHandler> logger)
        {
            _context = context;
            _tokenService = tokenService;
            _logger = logger;
        }


        public async Task<Response<TokenDto>> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var user = await _context.Users.Where(x => !x.IsDeleted && x.Username.Trim().ToLower() == request.Username.Trim().ToLower()).FirstOrDefaultAsync();
                if (user == null)
                {
                    _logger.LogWarning("Kullanıcı bulunamadı:{Username}" , request.Username);
                    return Response<TokenDto>.Fail(500, "kullanıcı bulunamadı veya parola hatalı");
                }
                var passwordVerificationResult = new PasswordHasher<User>().VerifyHashedPassword(user, user.PasswordHash, request.Password);
                if (passwordVerificationResult == PasswordVerificationResult.Failed)
                {
                    _logger.LogWarning("Parola hatalı:{Username}" , request.Username);
                    return Response<TokenDto>.Fail(500, "kullanıcı bulunamadı veya parola hatalı");
                }
                else
                {
                    var token = await _tokenService.CreateTokens(user);
                    _logger.LogInformation("Kullanıcı başarıyla giriş yaptı:{user.Username}" , user.Username);
                    return Response<TokenDto>.Success(200, token);
                    
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Kullanıcı giriş yaparken hata oluştu:{Error}" , ex.Message);
                return Response<TokenDto>.Fail(500, "beklenmedik bir hata oluştu "+ex);
            }
           
        }

        public async Task<Response<NoContent>> Handle(RegisterCommand request, CancellationToken cancellationToken)
        {
            try
             {
             var userExists = _context.Users.Any(u => u.Username.Trim().ToLower() == request.Username.Trim().ToLower());
                if (userExists) {
                    _logger.LogWarning("Kullanıcı zaten mevcut:{Username}" , request.Username);
                    return Response<NoContent>.Fail(500, "kullanıcı zaten mevcut");
                }
                else
                {
                    var user = new User();
                    user.Username = request.Username.Trim();
                    user.Email = request.Email.Trim();
                    user.PasswordHash = new PasswordHasher<User>().HashPassword(user, request.Password.Trim());
                    user.Role=request.Role.Trim()   ;
                    user.IsDeleted = false;
                    
                    _context.Users.Add(user);
                    await  _context.SaveChangesAsync();
                    _logger.LogInformation("Kullanıcı başarıyla kaydedildi:{Username}" , request.Username);
                    return Response<NoContent>.Success(200);
                }

            }
            catch (Exception ex)
            {
                _logger.LogError("Kullanıcı kaydederken hata oluştu:{Error}" , ex.Message);
                return Response<NoContent>.Fail(500, "beklenmedik bir hata oluştu "+ ex);
            }
        }

        public async Task<Response<TokenDto>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var user =await _context.Users.Where(x => !x.IsDeleted && x.Id == request.UserId).FirstOrDefaultAsync(cancellationToken);
                if (user == null)
                {
                    _logger.LogWarning("Kullanıcı bulunamadı:{UserId}" , request.UserId);
                    return Response<TokenDto>.Fail(500, "kullanıcı bulunamadı");
                }
                var token = await _tokenService.RefreshTokenAsync(user.Id,user.RefreshToken,cancellationToken);
                _logger.LogInformation("Token başarıyla yenilendi:{UserId}" , request.UserId);
                return Response<TokenDto>.Success(200, token);

            }
            catch (Exception ex)
            {
                _logger.LogError("Token yenilenirken hata oluştu:{Error}" , ex.Message);
                return Response<TokenDto>.Fail(500, "beklenmedik bir hata oluştu "+ ex);
            }
        }

        public async Task<Response<NoContent>> Handle(DeleteUser request, CancellationToken cancellationToken)
        {
            try
            {
                var user = await _context.Users.Where(x => !x.IsDeleted && x.Id == request.Id).FirstOrDefaultAsync(cancellationToken);
                if (user == null)
                {
                    _logger.LogWarning("Kullanıcı bulunamadı:{UserId}" , request.Id);
                    return Response<NoContent>.Fail(500, "kullanıcı bulunamadı");
                }
                user.IsDeleted = true;
                _context.Users.Update(user);
                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Kullanıcı başarıyla silindi:{UserId}" , request.Id);
                return Response<NoContent>.Success(200);

            }
            catch (Exception ex)
            {
                _logger.LogError("Kullanıcı silinirken hata oluştu:{Error}" , ex.Message);
                return Response<NoContent>.Fail(500, "beklenmedik bir hata oluştu "+ ex);

            }
        }

        public async Task<Response<NoContent>> Handle(LogoutCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var user =  _context.Users.Where(x => !x.IsDeleted && x.Id == request.UserId&& x.RefreshToken==request.RefreshToken).FirstOrDefault();
                if (user == null)
                {
                    _logger.LogWarning("Kullanıcı bulunamadı:{UserId}" , request.UserId);
                    return Response<NoContent>.Fail(400, "kullanıcı bulunamadı");
                }
                user.RefreshToken = null;
                user.RefreshTokenExpireDate=null;
                _context.Users.Update(user);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Kullanıcı başarıyla çıkış yaptı:{UserId}" , request.UserId);
                return Response<NoContent>.Success(204);

            }
            catch (Exception ex)
            {
                _logger.LogError("Kullanıcı çıkış yaparken hata oluştu:{Error}" , ex.Message);
                return Response<NoContent>.Fail(500, "beklenmedik bir hata oluştu "+ ex);

            }
        }
    }
}
