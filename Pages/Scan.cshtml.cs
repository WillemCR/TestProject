using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using TestProject.Data;
using TestProject.Models;

namespace TestProject.Pages
{
    public class ScanModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ScanModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<Product> Products { get; set; } = new List<Product>();

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                Products = await _context.Products
                    .OrderBy(p => p.volgorde)
                    .ToListAsync();

                return Page();
            }
            catch (Exception ex)
            {
                // Log the error here if you have logging configured
                return StatusCode(500, "Error loading products");
            }
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostScanAsync(string barcode)
        {
            if (string.IsNullOrEmpty(barcode))
            {
                return new JsonResult(new { 
                    success = false,
                    message = "Barcode is required",
                    statusCode = 400
                });
            }

            try
            {
                if (!int.TryParse(barcode, out int barcodeId))
                {
                    return new JsonResult(new { 
                        success = false,
                        message = "Invalid barcode format. Please use a valid product ID.",
                        statusCode = 400
                    });
                }

                var product = await _context.Products
                    .FirstOrDefaultAsync(p => p.id == barcodeId);

                if (product == null)
                {
                    return new JsonResult(new { 
                        success = false,
                        message = "Product not found",
                        statusCode = 404
                    });
                }

                if (product.gescand)
                {
                    return new JsonResult(new { 
                        success = false,
                        message = "Product already scanned",
                        productId = product.id,
                        statusCode = 400
                    });
                }

                product.gescand = true;
                var saveResult = await _context.SaveChangesAsync();

                if (saveResult > 0)
                {
                    return new JsonResult(new { 
                        success = true,
                        message = "Product scanned successfully",
                        productId = product.id,
                        statusCode = 200
                    });
                }
                else
                {
                    return new JsonResult(new { 
                        success = false,
                        message = "Failed to update product scan status",
                        productId = product.id,
                        statusCode = 500
                    });
                }
            }
            catch (Exception ex)
            {
                return new JsonResult(new { 
                    success = false,
                    message = $"Error processing scan: {ex.Message}",
                    statusCode = 500
                });
            }
        }
    }
}
