using System.Net.Mail;
using System.Threading.Tasks;

namespace TestProject.Services
{
    public interface IEmailSender
    {
        Task SendEmailAsync(string email, string subject, string message);
    }

    public class EmailSender : IEmailSender
    {
        public async Task SendEmailAsync(string email, string subject, string message)
        {
            try
            {
                Console.WriteLine($"Attempting to send email to: {email}");
                
                // For development, only allow sending to local Papercut SMTP
               
                using var client = new SmtpClient("localhost", 25)
                {
                    EnableSsl = false,
                    UseDefaultCredentials = false,
                    DeliveryMethod = SmtpDeliveryMethod.Network
                };
                
                using var mailMessage = new MailMessage
                {
                    From = new MailAddress("noreply@localhost.com"),
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
                Console.WriteLine($"SMTP Error: {ex.Message}");
                throw new Exception($"Failed to send email: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"General Error: {ex.Message}");
                throw;
            }
        }
    }
}
