using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using TestProject.Data;
using TestProject.Models;

namespace TestProject.Pages
{
    public class VehicleIndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public VehicleIndexModel(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public IList<Vehicle> Vehicles { get; private set; }

        public async Task<IActionResult> OnGetAsync()
        {
            Vehicles = await _context.Vehicles
                .AsNoTracking()
                .ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var vehicle = await _context.Vehicles
                .FirstOrDefaultAsync(v => v.Id == id);

            if (vehicle == null)
            {
                return RedirectToPage("/Vehicle");
            }

            try
            {
                _context.Vehicles.Remove(vehicle);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Vehicle deleted successfully.";
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Error deleting vehicle. Please try again.";
            }

            return RedirectToPage("/Vehicle");
        }
    }
}