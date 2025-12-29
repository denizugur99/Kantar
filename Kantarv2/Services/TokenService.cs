using Kantarv2.DAL;
using Kantarv2.Dtos;
using Kantarv2.Entities;
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
        public TokenService(IConfiguration configuration, KantarDbContext context)
        {
            _context = context;
            _configuration = configuration;
        }
        private string GenerateToken(User user)
        {
            var claims = new List<Claim>()
            {
                new Claim(ClaimTypes.NameIdentifier,user.Id.ToString()),
                new Claim(ClaimTypes.Name,user.Username),
                new Claim(ClaimTypes.Role,user.Role)
            };
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration.GetValue<string>("Appsettings:Token")));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);
            var tokenDescriptor = new JwtSecurityToken(
                issuer: _configuration.GetValue<string>("Appsettings:Issuer"),
                audience: _configuration.GetValue<string>("Appsettings:Audience"),
                claims: claims,
                expires: DateTime.Now.AddDays(1),
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
        private async Task<User?> ValidateTokenAsync(int userid, string refreshtoken)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userid);
            if (user == null || user.RefreshToken != refreshtoken || user.RefreshTokenExpireDate <= DateTime.Now)
            {
                return null;
            }
            return user;

        }
        private async Task<string> GenerateRefreshTokenAsync(User user)
        {
            var refreshToken = GenerateRefreshToken();
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpireDate = DateTime.Now.AddDays(1);
            await _context.SaveChangesAsync();
            return refreshToken;
        }
        public async Task<TokenDto?> RefreshTokenAsync(int userid, string refreshtoken,CancellationToken cancellationToken)
        {
            var user = await ValidateTokenAsync(userid, refreshtoken);
            if (user == null)
            {
                return null;
            }
            return await CreateTokens(user);
        }
        public async Task<TokenDto> CreateTokens(User? user)
        {
            var token = GenerateToken(user);
            var refreshToken = await GenerateRefreshTokenAsync(user);
            return new TokenDto
            {
                Token = token,
                RefreshToken = refreshToken
            };
        }
    }
}
   
