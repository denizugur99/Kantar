using Kantarv2.Dtos;
using Kantarv2.Entities;

namespace Kantarv2.Services
{
    public interface ITokenServiceInterface
    {
     
        Task<TokenDto> CreateTokens(User? user);
        Task<TokenDto> RefreshTokenAsync(int userid, string refreshtoken,CancellationToken cancellationToken);
       
    }
}
