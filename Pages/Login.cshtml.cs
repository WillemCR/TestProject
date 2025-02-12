using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;
using TestProject.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using TestProject.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace TestProject.Pages
{
    public class LoginModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public LoginModel(ApplicationDbContext context)
        {
            _context = context;
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

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Name == Name);
            if (user == null)
            {
                ModelState.AddModelError("", "Invalid username");
                return Page();
            }

            await SignInUser(user);

            return RedirectToPage("/Index");
        }

        /// <summary>
        /// Handles the logout operation
        /// </summary>
        /// <returns>A JSON result indicating successful logout</returns>
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostLogoutHandlerAsync()
        {
            await HttpContext.SignOutAsync("Cookies");
            return new JsonResult(new { success = true });
        }

        /// <summary>
        /// Handles user selection for login
        /// </summary>
        /// <param name="userId">The ID of the user to sign in as</param>
        /// <returns>A JSON result indicating success or failure</returns>
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostSelectUserAsync([FromForm] int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return BadRequest("User not found");
            }

            await SignInUser(user);
            return new JsonResult(new { success = true });
        }

        /// <summary>
        /// Signs in the specified user using claims-based authentication
        /// </summary>
        /// <param name="user">The user to authenticate</param>
        
        private async Task SignInUser(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Role, user.Role.ToString())
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTime.UtcNow.AddMinutes(30)
                }
            );
        }
    }
}
