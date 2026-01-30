using Microsoft.AspNetCore.Identity;

namespace Kantarv2.Entities
{
    public class User : IdentityUser<Guid>
    {
        // Custom properties beyond IdentityUser
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpireDate { get; set; }
        public int RefreshTokenVersion { get; set; } = 0;
        public bool IsDeleted { get; set; }

        // Note: Id, UserName, Email, PasswordHash are inherited from IdentityUser
        // EmailConfirmed, PhoneNumber, PhoneNumberConfirmed, TwoFactorEnabled, etc. are also available
        // SecurityStamp is also inherited - used for invalidating all tokens on password change, logout, etc.
    }
}
