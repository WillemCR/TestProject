using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using TestProject.Models;
using TestProject.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;

namespace TestProject.Pages
{
    [Authorize]
    public class ChangePasswordRequiredModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public ChangePasswordRequiredModel(ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Nieuw wachtwoord is verplicht")]
            [DataType(DataType.Password)]
            public string NewPassword { get; set; }

            [Required(ErrorMessage = "Bevestig wachtwoord is verplicht")]
            [DataType(DataType.Password)]
            [Compare("NewPassword", ErrorMessage = "Wachtwoorden komen niet overeen")]
            public string ConfirmPassword { get; set; }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            // Change password
            var result = await _userManager.RemovePasswordAsync(user);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return Page();
            }

            result = await _userManager.AddPasswordAsync(user, Input.NewPassword);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return Page();
            }

            // Update MustChangePassword flag
            user.MustChangePassword = false;
            await _context.SaveChangesAsync();

            return RedirectToPage("/Index");
        }
    }
}
