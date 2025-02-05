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

        public List<User> Users { get; set; } = new List<User>();

        public async Task OnGetAsync()
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
            
            await SignInUser(user);
            return RedirectToPage("/Index");
        }

       

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostLogoutHandlerAsync()
        {
            await HttpContext.SignOutAsync("Cookies");
            return new JsonResult(new { success = true});
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostSelectUserAsync([FromForm] int userId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                return BadRequest("User not found");
            }
            
            await SignInUser(user);
            return new JsonResult(new { success = true });
        }

        private async Task SignInUser(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim("IsAdmin", user.IsAdmin.ToString())
            };

            var identity = new ClaimsIdentity(claims, "Cookies");
            var principal = new ClaimsPrincipal(identity);
            
            await HttpContext.SignInAsync("Cookies", principal, new AuthenticationProperties
            {
                IsPersistent = true
            });
        }
    }
}