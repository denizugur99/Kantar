using Kantarv2.Messages;
using MassTransit;

namespace Kantarv2.Consumers
{
    public class UserCreatedConsumer : IConsumer<UserCreatedMessage>
    {
        private readonly ILogger<UserCreatedConsumer> _logger;

        public UserCreatedConsumer(ILogger<UserCreatedConsumer> logger)
        {
            _logger = logger;
        }

        public Task Consume(ConsumeContext<UserCreatedMessage> context)
        {
            var message = context.Message;

            _logger.LogInformation(
                "User created event received: UserId={UserId}, Username={Username}, Email={Email}",
                message.UserId,
                message.Username,
                message.Email);

            // Burada email gönderme, bildirim oluşturma vb. işlemler yapılabilir

            return Task.CompletedTask;
        }
    }
}
