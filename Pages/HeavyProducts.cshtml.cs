using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using TestProject.Data;
using TestProject.Models;

public class HeavyProductIndexModel : PageModel
{
    public List<HeavyProduct> HeavyProducts { get; set; } = new List<HeavyProduct>();

    private readonly ApplicationDbContext _context;

        public HeavyProductIndexModel(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

    public async Task OnGetAsync()
    {
        HeavyProducts = await _context.HeavyProducts.ToListAsync();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var product = await _context.HeavyProducts.FindAsync(id);
        if (product != null)
        {
            _context.HeavyProducts.Remove(product);
            await _context.SaveChangesAsync();
        }
        return RedirectToPage();
    }
}

public class HeavyProduct
{
    public int Id { get; set; }
    public string Name { get; set; }
}