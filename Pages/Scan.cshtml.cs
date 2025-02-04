using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
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
        public string Barcode { get; set; }

        public void OnGet()
        {
            Products = _context.Products.ToList();
        }

        public void OnPostScan(string barcode)
        {
            if (!string.IsNullOrEmpty(barcode))
            {
                var product = _context.Products.Find(barcode);
                if (product != null)
                {
                    product.gescand = true;
                    _context.SaveChanges();
                }
            }
        }
    }
}
