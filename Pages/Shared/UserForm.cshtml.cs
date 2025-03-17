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
using Microsoft.AspNetCore.Identity;

namespace TestProject.Pages.Shared
{
    [Authorize(Roles = "Admin")]
public class UserFormModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<User> _userManager;

    public UserFormModel(ApplicationDbContext context, UserManager<User> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    [BindProperty] 
    public User User { get; set; }  = new User();

  
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

    [BindProperty]
    public string? Password { get; set; }

    [BindProperty]
    public string? ConfirmPassword { get; set; }

    public async Task<IActionResult> OnPostAsync()
    {
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
                // Create new user
                var result = await _userManager.CreateAsync(User, Password!);
                if (!result.Succeeded)
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    await LoadSelectLists();
                    return Page();
                }
                
                // Set MustChangePassword flag
                User.MustChangePassword = true;
                await _context.SaveChangesAsync();
            }
            else
            {
                // Update existing user
                var existingUser = await _context.Users.FindAsync(User.Id);
                if (existingUser != null)
                {
                    existingUser.UserName = User.UserName;
                    existingUser.Email = User.Email;
                    existingUser.Role = User.Role;
                    existingUser.MustChangePassword = User.MustChangePassword;
                    
                    // Update password if provided
                    if (!string.IsNullOrEmpty(Password))
                    {
                        var token = await _userManager.GeneratePasswordResetTokenAsync(existingUser);
                        var result = await _userManager.ResetPasswordAsync(existingUser, token, Password);
                        if (!result.Succeeded)
                        {
                            foreach (var error in result.Errors)
                            {
                                ModelState.AddModelError(string.Empty, error.Description);
                            }
                            await LoadSelectLists();
                            return Page();
                        }
                    }
                    
                    _context.Update(existingUser);
                    await _context.SaveChangesAsync();
                }
            }

            return RedirectToPage("/Users");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception: {ex.Message}");
            ModelState.AddModelError("", "An error occurred while saving the user");
            await LoadSelectLists();
            return Page();
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
