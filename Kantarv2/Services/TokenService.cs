using Kantarv2.DAL;
using Kantarv2.Dtos;
using Kantarv2.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Kantarv2.Services
{
    public class TokenService : ITokenServiceInterface
    {
        private readonly IConfiguration _configuration;
        private readonly KantarDbContext _context;
        private readonly UserManager<User> _userManager;

        public TokenService(IConfiguration configuration, KantarDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _configuration = configuration;
            _userManager = userManager;
        }
        private async Task<string> GenerateToken(User user, UserManager<User> userManager)
        {
            // Get user roles from Identity
            var roles = await userManager.GetRolesAsync(user);

            var claims = new List<Claim>()
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.UserName ?? string.Empty),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                // SecurityStamp - token invalidation için kritik!
                new Claim("security_stamp", user.SecurityStamp ?? string.Empty),
                // RefreshTokenVersion - refresh token rotation için
                new Claim("refresh_token_version", user.RefreshTokenVersion.ToString())
            };

            // Add all roles as claims
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration.GetValue<string>("Appsettings:Token")));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);
            var tokenDescriptor = new JwtSecurityToken(
                issuer: _configuration.GetValue<string>("Appsettings:Issuer"),
                audience: _configuration.GetValue<string>("Appsettings:Audience"),
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(15), // 1 gün -> 15 dakika
                signingCredentials: creds
                );
            return new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);
        }
        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
                return Convert.ToBase64String(randomNumber);
            }
        }
        private async Task<User?> ValidateRefreshTokenAsync(Guid userid, string refreshtoken)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userid && !u.IsDeleted);

            if (user == null)
            {
                return null;
            }

            // Refresh token kontrolü
            if (user.RefreshToken != refreshtoken)
            {
                return null;
            }

            // Expiration kontrolü
            if (user.RefreshTokenExpireDate <= DateTime.UtcNow)
            {
                return null;
            }

            return user;
        }

        private async Task<string> GenerateAndSaveRefreshTokenAsync(User user)
        {
            var refreshToken = GenerateRefreshToken();

            // ROTATION: Her yeni refresh token generation'da version artır
            user.RefreshTokenVersion++;
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpireDate = DateTime.UtcNow.AddDays(30); // 1 gün -> 30 gün

            await _userManager.UpdateAsync(user);

            return refreshToken;
        }

        public async Task<TokenDto?> RefreshTokenAsync(Guid userid, string refreshtoken, CancellationToken cancellationToken)
        {
            // Validate refresh token
            var user = await ValidateRefreshTokenAsync(userid, refreshtoken);

            if (user == null)
            {
                return null;
            }

            // ROTATION: Yeni token + yeni refresh token üret
            // Eski refresh token otomatik geçersiz olur
            return await CreateTokens(user);
        }
        public async Task<TokenDto> CreateTokens(User? user)
        {
            var token = await GenerateToken(user, _userManager);
            var refreshToken = await GenerateAndSaveRefreshTokenAsync(user);

            return new TokenDto
            {
                Token = token,
                RefreshToken = refreshToken
            };
        }
    }
}
   
