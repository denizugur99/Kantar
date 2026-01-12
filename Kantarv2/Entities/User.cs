using Microsoft.AspNetCore.Identity;

namespace Kantarv2.Entities
{
    public class User : IdentityUser<int>
    {
        // Custom properties beyond IdentityUser
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpireDate { get; set; }
        public bool IsDeleted { get; set; }

        // Note: Id, UserName, Email, PasswordHash are inherited from IdentityUser
        // EmailConfirmed, PhoneNumber, PhoneNumberConfirmed, TwoFactorEnabled, etc. are also available
    }
}
