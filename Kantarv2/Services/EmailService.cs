using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace Kantarv2.Services
{
    public class EmailSettings
    {
        public string SmtpServer { get; set; } = string.Empty;
        public int SmtpPort { get; set; }
        public string SenderEmail { get; set; } = string.Empty;
        public string SenderName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool EnableSsl { get; set; }
    }

    public class EmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<EmailSettings> emailSettings, ILogger<EmailService> logger)
        {
            _emailSettings = emailSettings.Value;
            _logger = logger;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                using var smtpClient = new SmtpClient(_emailSettings.SmtpServer, _emailSettings.SmtpPort)
                {
                    Credentials = new NetworkCredential(_emailSettings.SenderEmail, _emailSettings.Password),
                    EnableSsl = _emailSettings.EnableSsl
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_emailSettings.SenderEmail, _emailSettings.SenderName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };
                mailMessage.To.Add(toEmail);

                await smtpClient.SendMailAsync(mailMessage);
                _logger.LogInformation("Email gonderildi: {ToEmail}", toEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError("Email gonderilirken hata: {Error}", ex.Message);
                throw;
            }
        }

        public async Task SendPasswordResetEmailAsync(string toEmail, string resetToken, string userName)
        {
            var subject = "Sifre Sifirlama Talebi - Kantar";
            var body = $@"
                <html>
                <body style='font-family: Arial, sans-serif;'>
                    <h2>Merhaba {userName},</h2>
                    <p>Sifrenizi sifirlamak icin asagidaki kodu kullanin:</p>
                    <div style='background-color: #f4f4f4; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                        <strong style='font-size: 24px; color: #333;'>{resetToken}</strong>
                    </div>
                    <p>Bu kod 1 saat gecerlidir.</p>
                    <p>Eger bu talebi siz yapmadiyseniz, bu emaili dikkate almayin.</p>
                    <br>
                    <p>Saygilarimizla,<br>Kantar Ekibi</p>
                </body>
                </html>";

            await SendEmailAsync(toEmail, subject, body);
        }
    }
}
