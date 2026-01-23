using Microsoft.AspNetCore.Identity.UI.Services;
using System.Net;
using System.Net.Mail;

namespace BankApp_Web.Services
{
    // E-mail service voor Identity Framework
    // Gebruikt SMTP om e-mails te versturen
    public class EmailService : IEmailSender
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        // Verstuur e-mail (Identity Framework interface)
        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            try
            {
                // SMTP instellingen uit appsettings.json
                var smtpServer = _configuration["Email:SmtpServer"] ?? "smtp.gmail.com";
                var smtpPort = int.Parse(_configuration["Email:SmtpPort"] ?? "587");
                var smtpUser = _configuration["Email:SmtpUser"] ?? "";
                var smtpPassword = _configuration["Email:SmtpPassword"] ?? ""; // Onzichtbare toegangscode
                var fromEmail = _configuration["Email:FromEmail"] ?? smtpUser;
                var fromName = _configuration["Email:FromName"] ?? "BankApp";

                // Maak SMTP client
                using var client = new SmtpClient(smtpServer, smtpPort)
                {
                    EnableSsl = true,
                    Credentials = new NetworkCredential(smtpUser, smtpPassword)
                };

                // Maak e-mail bericht
                var mailMessage = new MailMessage
                {
                    From = new MailAddress(fromEmail, fromName),
                    Subject = subject,
                    Body = htmlMessage,
                    IsBodyHtml = true
                };

                mailMessage.To.Add(email);

                // Verstuur e-mail
                await client.SendMailAsync(mailMessage);

                _logger.LogInformation($"E-mail verstuurd naar {email}: {subject}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Fout bij versturen e-mail naar {email}: {ex.Message}");
                // In development: log maar gooi geen exception (voor testen)
                // In production: zou je kunnen beslissen om exception te gooien
            }
        }
    }
}
