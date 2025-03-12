using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch.Internal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using TestProject.Data;
using TestProject.Models;

namespace TestProject.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Laadploeg, Planner, Admin")]
    public class ScanApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        
        public ScanApiController(ApplicationDbContext context)
        {
            _context = context;
        }
        
        [HttpPost("ProcessBarcode")]
        public async Task<IActionResult> ProcessBarcode([FromBody] ScanRequest request)
        {
            try
            {
                Console.WriteLine("ProcessBarcode API endpoint reached");
               
                if (request == null || string.IsNullOrEmpty(request.Barcode))
                {
                    return BadRequest(new { success = false, message = "Barcode is verplicht" });
                }
                var product = await _context.Products
                    .FirstOrDefaultAsync(p => p.orderregelnummer == request.Barcode);
               
                if (product == null)
                {
                    return NotFound(new { success = false, message = "Product niet gevonden" });
                }
                product.aantal += 1;
                bool isComplete = product.aantal == int.Parse(product.colli);
                if (isComplete)
                {
                    product.gescand = true;
                }
           
                await _context.SaveChangesAsync();
                
                // Check if all products for this vehicle are scanned
                bool allProductsScanned = false;
                if (isComplete && !string.IsNullOrEmpty(product.voertuig))
                {
                    // Get all products for this vehicle
                    var vehicleProducts = await _context.Products
                        .Where(p => p.voertuig == product.voertuig)
                        .ToListAsync();
                    
                    // Check if all products are either scanned or reported as missing
                    allProductsScanned = vehicleProducts.All(p => p.gescand || p.gemeld);
                    
                    if (allProductsScanned)
                    {
                        // Log that all products are scanned
                        Console.WriteLine($"All products for vehicle {product.voertuig} are scanned or reported.");
                    }
                }
               
                // Return additional information
                return Ok(new {
                    success = true,
                    message = "Product succesvol gescand",
                    aantal = product.aantal,
                    colli = int.Parse(product.colli),
                    isComplete = isComplete,
                    allProductsScanned = allProductsScanned
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"API Exception: {ex.Message}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost("ReportMissing")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReportMissing([FromForm] MissingProductReport report)
        {
            try
            {
                // Create a new missing product report entry
                var missingReport = new MissingProductReportEntity
                {
                    OrderregelNummer = report.OrderregelNummer,
                    Artikelomschrijving = report.Artikelomschrijving,
                    Reason = report.Reason,
                    Comments = report.Comments,
                    ReportedAt = DateTime.Now,
                    ReportedBy = User.Identity.Name
                };
                
                // Add to database
                _context.MissingProductReports.Add(missingReport);
                
                // Update the product's gemeld status
                var product = await _context.Products
                    .FirstOrDefaultAsync(p => p.orderregelnummer == report.OrderregelNummer);
                
                bool allProductsScanned = false;
                
                if (product != null)
                {
                    product.gemeld = true;
                    
                    // Check if all products for this vehicle are scanned
                    if (!string.IsNullOrEmpty(product.voertuig))
                    {
                        // Get all products for this vehicle
                        var vehicleProducts = await _context.Products
                            .Where(p => p.voertuig == product.voertuig)
                            .ToListAsync();
                        
                        // Check if all products are either scanned or reported as missing
                        allProductsScanned = vehicleProducts.All(p => p.gescand || p.gemeld || p.orderregelnummer == report.OrderregelNummer);
                        
                        if (allProductsScanned)
                        {
                            // Log that all products are scanned
                            Console.WriteLine($"All products for vehicle {product.voertuig} are scanned or reported.");
                        }
                    }
                }
                
                await _context.SaveChangesAsync();
               
                return Ok(new { success = true, allProductsScanned = allProductsScanned });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }
        
        public class ScanRequest
        {
            public string Barcode { get; set; }
        }
        
        public class MissingProductReport
        {
            public string OrderregelNummer { get; set; }
            public string Artikelomschrijving { get; set; }
            public string Reason { get; set; }
            public string Comments { get; set; }
        }
    }
}
