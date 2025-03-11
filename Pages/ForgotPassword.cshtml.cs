using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using TestProject.Models;
using TestProject.Services;
using System.Net.Mail;

namespace TestProject.Pages
{
    public class ForgotPasswordModel : PageModel
    {
        private readonly UserManager<User> _userManager;
        private readonly IEmailSender _emailSender;

        public ForgotPasswordModel(UserManager<User> userManager, IEmailSender emailSender)
        {
            _userManager = userManager;
            _emailSender = emailSender;
        }

        [BindProperty]
        public string Email { get; set; }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrEmpty(Email))
            {
                ModelState.AddModelError(string.Empty, "E-mailadres is verplicht");
                return Page();
            }

            // Normalize email address
                     
            var user = await _userManager.Users
                .FirstOrDefaultAsync(u => u.Email == Email);
            if (user == null)
            {
                Console.WriteLine($"User not found for email: {Email}");
                return RedirectToPage("ForgotPasswordConfirmation");
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var callbackUrl = Url.Page(
                "/ResetPassword",
                pageHandler: null,
                values: new { userId = user.Id, token },
                protocol: Request.Scheme);

            Console.WriteLine($"Generated password reset URL: {callbackUrl}");

            try
            {
                await _emailSender.SendEmailAsync(
                    Email,
                    "Wachtwoord resetten",
                    $"Klik <a href='{callbackUrl}'>hier</a> om uw wachtwoord te resetten.");
                    
                Console.WriteLine("Email sent successfully");
                return RedirectToPage("ForgotPasswordConfirmation");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending email: {ex.Message}");
                ModelState.AddModelError(string.Empty, "Er is een fout opgetreden bij het versturen van de e-mail. Probeer het later opnieuw.");
                return Page();
            }
        }
    }
}
