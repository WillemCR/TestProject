using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TestProject.Models;
using TestProject.Data;
using System.Threading.Tasks;

namespace TestProject.Pages.Shared
{
    public class VehicleCreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public VehicleCreateModel(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        [BindProperty]
        public Vehicle Vehicle { get; set; }

        public IActionResult OnGet()
        {
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                _context.Vehicles.Add(Vehicle);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Vehicle created successfully.";
                return RedirectToPage("/Vehicle");
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError("", "Error saving vehicle. Please try again or contact support.");
            }
            catch (Exception)
            {
                ModelState.AddModelError("", "An unexpected error occurred. Please try again or contact support.");
            }

            return Page();
        }
    }
}