using Kantarv2.Messages;
using Kantarv2.Services;
using MassTransit;

namespace Kantarv2.Consumers
{
    public class PasswordResetEmailConsumer : IConsumer<PasswordResetMessage>
    {
        private readonly IEmailService _emailService;
        private readonly ILogger<PasswordResetEmailConsumer> _logger;

        public PasswordResetEmailConsumer(IEmailService emailService, ILogger<PasswordResetEmailConsumer> logger)
        {
            _emailService = emailService;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<PasswordResetMessage> context)
        {
            var message = context.Message;

            _logger.LogInformation(
                "Password reset email request received: Email={Email}, UserName={UserName}",
                message.Email,
                message.UserName);

            try
            {
                await _emailService.SendPasswordResetEmailAsync(
                    message.Email,
                    message.ResetToken,
                    message.UserName);

                _logger.LogInformation("Password reset email sent successfully to {Email}", message.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send password reset email to {Email}", message.Email);
                throw;
            }
        }
    }
}
