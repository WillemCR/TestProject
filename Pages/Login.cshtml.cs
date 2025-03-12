using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;
using TestProject.Models;
using TestProject.Data;
using Microsoft.EntityFrameworkCore;

namespace TestProject.Pages
{
    public class LoginModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;

        public LoginModel(
            ApplicationDbContext context,
            UserManager<User> userManager,
            SignInManager<User> signInManager)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public IList<User> Users { get; set; } = new List<User>();

        /// <summary>
        /// Loads the list of users when the page is accessed
        /// </summary>
        public async Task OnGetAsync()
        {
            Users = await _context.Users.ToListAsync();
        }

        [BindProperty]
        public string Name { get; set; }

        [BindProperty]
        public string Password { get; set; }

        /// <summary>
        /// Handles the login form submission
        /// </summary>
        /// <returns>A redirect to the Index page if successful, otherwise returns to the Login page</returns>
        public async Task<IActionResult> OnPostHandleAsync()
        {
            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("", "Invalid input");
                return Page();
            }

            var user = await _userManager.FindByNameAsync(Name);
            if (user == null)
            {
                ModelState.AddModelError("", "Invalid username");
                return Page();
            }

            var result = await _signInManager.PasswordSignInAsync(user, Password, isPersistent: true, lockoutOnFailure: false);
            if (!result.Succeeded)
            {
                ModelState.AddModelError("", "Invalid login attempt");
                return Page();
            }

            return RedirectToPage("/Index");
        }

        /// <summary>
        /// Handles the logout operation
        /// </summary>
        /// <returns>A JSON result indicating successful logout</returns>
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostLogoutHandlerAsync()
        {
            await _signInManager.SignOutAsync();
            return new JsonResult(new { success = true });
        }

        /// <summary>
        /// Handles user selection for login
        /// </summary>
        /// <param name="userId">The ID of the user to sign in as</param>
        /// <param name="password">The password for the user</param>
        /// <returns>A JSON result indicating success or failure</returns>
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostSelectUserAsync([FromForm] int userId, [FromForm] string password)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return BadRequest("User not found");
            }

            var result = 
            await _signInManager.PasswordSignInAsync(user, password, isPersistent: true, lockoutOnFailure: false);
            if (!result.Succeeded)
            {
                return new JsonResult(new { success = false });
            }

            return new JsonResult(new { success = true });
        }
    }
}