namespace Kantarv2.Messages
{
    public record UserCreatedMessage
    {
        public Guid UserId { get; init; }
        public string Username { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public DateTime CreatedAt { get; init; }
    }
}
