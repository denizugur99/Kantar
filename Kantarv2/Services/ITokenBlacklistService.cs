namespace Kantarv2.Services
{
    public interface ITokenBlacklistService
    {
        /// <summary>
        /// Token'ı blacklist'e ekler (logout işlemi için)
        /// </summary>
        Task BlacklistTokenAsync(string token, TimeSpan expiration);

        /// <summary>
        /// Token'ın blacklist'te olup olmadığını kontrol eder
        /// </summary>
        Task<bool> IsTokenBlacklistedAsync(string token);
    }
}
