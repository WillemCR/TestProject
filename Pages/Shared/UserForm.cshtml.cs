using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using TestProject.Models;
using TestProject.Data;
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
    public User User { get; set; }  = new User();

    public IEnumerable<SelectListItem> Crews { get; set; }
    public IEnumerable<SelectListItem> Roles { get; set; }

    public async Task OnGetAsync(int id = 0)
    {
        User = id == 0 ? new User() : await _context.Users.FindAsync(id);
        
        // Get enum values from UserRole enum
        var roles = new List<SelectListItem>();
        foreach (string roleName in Enum.GetNames(typeof(UserRole)))
        {
            roles.Add(new SelectListItem {
                Value = roleName,
                Text = roleName
            });
        }
        Roles = roles;
        
    }

    public async Task<IActionResult> OnPostAsync()
{
    // Add debugging to check what's being received
    Console.WriteLine($"User object: {User?.Id}");
    Console.WriteLine($"ModelState valid: {ModelState.IsValid}");
    
    try 
    {
        if (!ModelState.IsValid)
        {
            await LoadSelectLists();
            return Page();
        }

        if (User == null) 
        {
            ModelState.AddModelError("", "User data not received");
            await LoadSelectLists();
            return Page();
        }

        if (User.Id == 0)
        {
            _context.Users.Add(User);
        }
        else
        {
            var existingUser = await _context.Users.FindAsync(User.Id);
            if (existingUser != null)
            {
                existingUser.UserName = User.UserName;
                existingUser.Email = User.Email;
                existingUser.Role = User.Role;
                existingUser.PasswordHash = existingUser.PasswordHash; // Preserve existing password hash
                _context.Update(existingUser);
            }
        }

        await _context.SaveChangesAsync();
        return RedirectToPage("/Users");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Exception: {ex.Message}");
        throw;
    }
}

// Add this helper method
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
    }
}
}
