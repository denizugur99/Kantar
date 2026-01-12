using Microsoft.Extensions.Caching.Distributed;
using System.Security.Cryptography;
using System.Text;

namespace Kantarv2.Services
{
    public class TokenBlacklistService : ITokenBlacklistService
    {
        private readonly IDistributedCache _cache;
        private readonly ILogger<TokenBlacklistService> _logger;
        private const string BLACKLIST_PREFIX = "blacklist_token_";

        public TokenBlacklistService(IDistributedCache cache, ILogger<TokenBlacklistService> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        public async Task BlacklistTokenAsync(string token, TimeSpan expiration)
        {
            try
            {
                // Token'ı hash'leyerek Redis'e kaydet (güvenlik için)
                var hashedToken = HashToken(token);
                var key = BLACKLIST_PREFIX + hashedToken;

                var options = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = expiration
                };

                await _cache.SetStringAsync(key, "blacklisted", options);
                _logger.LogInformation("Token blacklist'e eklendi: {HashedToken}", hashedToken.Substring(0, 10) + "...");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Token blacklist'e eklenirken hata oluştu");
                throw;
            }
        }

        public async Task<bool> IsTokenBlacklistedAsync(string token)
        {
            try
            {
                var hashedToken = HashToken(token);
                var key = BLACKLIST_PREFIX + hashedToken;

                var value = await _cache.GetStringAsync(key);
                return !string.IsNullOrEmpty(value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Token blacklist kontrolü sırasında hata oluştu");
                // Hata durumunda güvenli tarafta kalmak için false dön
                return false;
            }
        }

        private string HashToken(string token)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(token);
                var hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }
    }
}
