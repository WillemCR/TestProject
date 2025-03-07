using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TestProject.Data;
using TestProject.Models;

namespace TestProject.Pages
{
    [Authorize(Roles = "Laadploeg, Planner, Admin")]
   public class ScanModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public ScanModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public IList<Product> HeavyProducts { get; set; } = new List<Product>();
    public IList<Product> RegularProducts { get; set; } = new List<Product>();
    public List<string> UniqueVehicles { get; set; } = new List<string>();
    public string SelectedVehicle { get; set; }

    public async Task OnGetAsync(string vehicle = null)
    {
        // Get unique vehicles
        UniqueVehicles = await _context.Products
            .Select(p => p.voertuig)
            .Distinct()
            .Where(v => v != null)
            .OrderBy(v => v)
            .ToListAsync();

        SelectedVehicle = vehicle;

        if (!string.IsNullOrEmpty(vehicle))
        {
            // Get all HeavyProduct names
            var heavyProductNames = await _context.HeavyProducts
                .Select(hp => hp.Name)
                .ToListAsync();

            // Get all products for the selected vehicle
            var allProducts = await _context.Products
                .Where(p => p.voertuig == vehicle)
                .OrderByDescending(p => p.volgorde)
                .ToListAsync();

            // Split products into heavy and regular
            HeavyProducts = allProducts
                .Where(p => heavyProductNames.Any(hp => p.artikelomschrijving != null && p.artikelomschrijving.Contains(hp)))
                .ToList();

            RegularProducts = allProducts
                .Where(p => !heavyProductNames.Any(hp => p.artikelomschrijving != null && p.artikelomschrijving.Contains(hp)))
                .ToList();
        }
    }
}
}