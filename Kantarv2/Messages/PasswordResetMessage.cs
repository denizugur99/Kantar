namespace Kantarv2.Messages
{
    public record PasswordResetMessage
    {
        public string Email { get; init; } = string.Empty;
        public string UserName { get; init; } = string.Empty;
        public string ResetToken { get; init; } = string.Empty;
        public DateTime CreatedAt { get; init; }
    }
}
