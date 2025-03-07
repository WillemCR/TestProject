using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TestProject.Models;
using TestProject.Data;
using System.Threading.Tasks;

public class HeavyProductFormModel : PageModel
{
    private readonly ApplicationDbContext _context;

    [BindProperty]
    public HeavyProduct HeavyProduct { get; set; }

    public HeavyProductFormModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null)
        {
            HeavyProduct = new HeavyProduct();
            return Page();
        }

        HeavyProduct = await _context.HeavyProducts.FindAsync(id);
        if (HeavyProduct == null)
        {
            return NotFound();
        }
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        if (HeavyProduct.Id == 0) // New product
        {
            _context.HeavyProducts.Add(HeavyProduct);
            await _context.SaveChangesAsync();
        }
        else // Existing product
        {
            var existingProduct = await _context.HeavyProducts.FindAsync(HeavyProduct.Id);
            if (existingProduct == null)
            {
                return NotFound();
            }

            // Explicitly update the existing product's name
            existingProduct.Name = HeavyProduct.Name;
            _context.Update(existingProduct); // Mark the entity as modified
            await _context.SaveChangesAsync();
        }

        return RedirectToPage("../HeavyProducts");
    }
}