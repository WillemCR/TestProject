using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using TestProject.Settings;
using System;

namespace TestProject.Services
{
    public interface IEmailSender
    {
        Task SendEmailAsync(string email, string subject, string message);
    }

    public class EmailSender : IEmailSender
    {
        private readonly EmailSettings _emailSettings;

        public EmailSender(IOptions<EmailSettings> emailSettingsOptions)
        {
            _emailSettings = emailSettingsOptions.Value;

            if (string.IsNullOrEmpty(_emailSettings.SmtpHost) || string.IsNullOrEmpty(_emailSettings.SenderAddress))
            {
                Console.WriteLine("WAARSCHUWING: Email instellingen (SmtpHost/SenderAddress) zijn niet volledig geconfigureerd in appsettings.json.");
            }
        }

        public async Task SendEmailAsync(string email, string subject, string message)
        {
            try
            {
                Console.WriteLine($"Attempting to send email to: {email} via {_emailSettings.SmtpHost}:{_emailSettings.SmtpPort} (SSL: {_emailSettings.EnableSsl})");
                
                using var client = new SmtpClient(_emailSettings.SmtpHost, _emailSettings.SmtpPort)
                {
                    EnableSsl = _emailSettings.EnableSsl,
                    UseDefaultCredentials = false,
                    DeliveryMethod = SmtpDeliveryMethod.Network
                };
                
                using var mailMessage = new MailMessage
                {
                    From = new MailAddress(_emailSettings.SenderAddress),
                    Subject = subject,
                    Body = message,
                    IsBodyHtml = true
                };
                mailMessage.To.Add(email);

                Console.WriteLine($"Sending email with subject: {subject}");
                await client.SendMailAsync(mailMessage);
                Console.WriteLine("Email sent successfully");
            }
            catch (SmtpException ex)
            {
                Console.WriteLine($"SMTP Error sending to {email} via {_emailSettings.SmtpHost}:{_emailSettings.SmtpPort} - {ex.Message}");
                throw new Exception($"Kon e-mail niet verzenden via {_emailSettings.SmtpHost}: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"General Error sending email: {ex.Message}");
                throw;
            }
        }
    }
}
