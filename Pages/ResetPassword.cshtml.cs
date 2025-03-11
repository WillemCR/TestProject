using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;
using TestProject.Models;

namespace TestProject.Pages
{
    public class ResetPasswordModel : PageModel
    {
        private readonly UserManager<User> _userManager;

        public ResetPasswordModel(UserManager<User> userManager)
        {
            _userManager = userManager;
        }

        [BindProperty]
        public string Password { get; set; }

        [BindProperty]
        public string ConfirmPassword { get; set; }

        [BindProperty(SupportsGet = true)]
        public string UserId { get; set; }

        [BindProperty(SupportsGet = true)]
        public string Token { get; set; }

        public IActionResult OnGet(string userId, string token)
        {
            if (userId == null || token == null)
            {
                return BadRequest("Ongeldige reset link");
            }

            UserId = userId;
            Token = token;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (Password != ConfirmPassword)
            {
                ModelState.AddModelError(string.Empty, "Wachtwoorden komen niet overeen");
                return Page();
            }

            var user = await _userManager.FindByIdAsync(UserId);
            if (user == null)
            {
                return RedirectToPage("ResetPasswordConfirmation");
            }

            var result = await _userManager.ResetPasswordAsync(user, Token, Password);
            if (result.Succeeded)
            {
                return RedirectToPage("ResetPasswordConfirmation");
            }

            foreach (var error in result.Errors)
            {
                // Translate common error messages to Dutch
                var message = error.Description switch
                {
                    "Passwords must be at least 6 characters." => "Wachtwoord moet minimaal 6 tekens bevatten",
                    "Passwords must have at least one non alphanumeric character." => "Wachtwoord moet minimaal één speciaal teken bevatten",
                    "Passwords must have at least one digit ('0'-'9')." => "Wachtwoord moet minimaal één cijfer bevatten",
                    "Passwords must have at least one uppercase ('A'-'Z')." => "Wachtwoord moet minimaal één hoofdletter bevatten",
                    _ => error.Description
                };
                ModelState.AddModelError(string.Empty, message);
            }
            return Page();
        }
    }
}
