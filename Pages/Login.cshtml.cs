using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using TestProject.Data;
using TestProject.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;

namespace TestProject.Pages
{
    public class LoginModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public LoginModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<User> Users { get; set; }

        public async Task OnGet()
        {
            Users = await _context.Users.ToListAsync();
        }

        [BindProperty]
        public string Name { get; set; }

        public async Task<IActionResult> OnPostHandleAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Name == Name);
            if (user == null)
            {
                ModelState.AddModelError("", "Invalid username");
                return Page();
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim("IsAdmin", user.IsAdmin.ToString())
            };

            var identity = new ClaimsIdentity(claims, "Cookies");
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync("Cookies", principal);
            
            return Redirect("/");
        }

        public async Task<IActionResult> OnPostLogoutAsync()
        {
            await HttpContext.SignOutAsync("Cookies");
            return RedirectToPage("/Login");
        }

        public async Task<IActionResult> OnPostSelectUserAsync(int userId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                return BadRequest("User not found");
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim("IsAdmin", user.IsAdmin.ToString())
            };

            var identity = new ClaimsIdentity(claims, "Cookies");
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync("Cookies", principal);
            
            return Redirect("/");
        }
    }
}