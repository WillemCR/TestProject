using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TestProject.Data;
using TestProject.Models;
using Microsoft.AspNetCore.Authorization;

namespace TestProject.Pages.Shared
{
    [Authorize(Roles = "Admin")]
    public class UserFormModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public UserFormModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty] 
        public User User { get; set; } = new User();

        public IEnumerable<SelectListItem> Crews { get; set; }
        public IEnumerable<SelectListItem> Roles { get; set; }

        public async Task OnGetAsync(int id = 0)
        {
            User = id == 0 ? new User() : await _context.Users.FindAsync(id);
            await LoadSelectLists();
        }

        public async Task<IActionResult> OnPostSaveAsync()
        {
            if (string.IsNullOrWhiteSpace(User.Name))
            {
                ModelState.AddModelError("User.Name", "Name is required");
            }
           

            if (!ModelState.IsValid)
            {
                await LoadSelectLists();
                return Page();
            }

            try 
            {
                if (User.Id == 0)
                {
                    _context.Users.Add(User);
                }
                else
                {
                    var existingUser = await _context.Users.FindAsync(User.Id);
                    if (existingUser == null)
                    {
                        ModelState.AddModelError("", "User not found");
                        await LoadSelectLists();
                        return Page();
                    }

                    _context.Entry(existingUser).CurrentValues.SetValues(User);
                }

                await _context.SaveChangesAsync();
                return RedirectToPage("/Users");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An error occurred while saving the user.");
                await LoadSelectLists();
                return Page();
            }
        }

        private async Task LoadSelectLists()
        {
            var roles = new List<SelectListItem>();
            foreach (string roleName in Enum.GetNames(typeof(UserRole)))
            {
                roles.Add(new SelectListItem 
                {
                    Value = roleName,
                    Text = roleName
                });
            }
            Roles = roles;
            
            Crews = await _context.Crews
                .Select(c => new SelectListItem 
                { 
                    Value = c.CrewId.ToString(), 
                    Text = c.Name 
                })
                .ToListAsync();
        }
    }
}
