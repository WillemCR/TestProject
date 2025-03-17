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
    /// <summary>
    /// API controller for handling product scanning operations
    /// Requires user to be in Laadploeg, Planner, or Admin role
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Laadploeg, Planner, Admin")]
    public class ScanApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        
        /// <summary>
        /// Constructor that injects the database context
        /// </summary>
        /// <param name="context">The application database context</param>
        public ScanApiController(ApplicationDbContext context)
        {
            _context = context;
        }
        
        /// <summary>
        /// Processes a barcode scan, updating the product count in the database
        /// </summary>
        /// <param name="request">The scan request containing the barcode</param>
        /// <returns>Result of the scan operation</returns>
        [HttpPost("ProcessBarcode")]
        public async Task<IActionResult> ProcessBarcode([FromBody] ScanRequest request)
        {
            try
            {
                Console.WriteLine("ProcessBarcode API endpoint reached");
               
                // Validate the request
                if (request == null || string.IsNullOrEmpty(request.Barcode))
                {
                    return BadRequest(new { success = false, message = "Barcode is verplicht" });
                }

                // Find the product with the matching barcode
                var product = await _context.Products
                    .FirstOrDefaultAsync(p => p.orderregelnummer == request.Barcode);
               
                // Return 404 if product not found
                if (product == null)
                {
                    return NotFound(new { success = false, message = "Product niet gevonden" });
                }
 
                // Check if we've already scanned the maximum number of this product
                if(!(product.colli > product.aantal + product.gemeld))
                {
                    return BadRequest(new { success = false, message = "Maximum aantal voor dit product is bereikt" });
                }

                // Increment the scanned count and check if complete
                product.aantal += 1;
                bool isComplete = product.aantal + product.gemeld == product.colli;
                if (isComplete)
                {
                    product.gescand = true;
                }
           
                // Save changes to the database
                await _context.SaveChangesAsync();
                
                // Check if all products for this vehicle are scanned or reported
                bool allProductsScanned = false;
                if (isComplete && !string.IsNullOrEmpty(product.voertuig))
                {
                    // Get all products for this vehicle
                    var vehicleProducts = await _context.Products
                        .Where(p => p.voertuig == product.voertuig)
                        .ToListAsync();
                    
                    // A product is considered processed if it's either scanned or reported as missing
                    allProductsScanned = vehicleProducts.All(p => p.gescand || p.gemeld > 0);
                    
                    if (allProductsScanned)
                    {
                        // Log that all products are scanned
                        Console.WriteLine($"All products for vehicle {product.voertuig} are scanned or reported.");
                    }
                }
               
                // Return success response with additional information
                return Ok(new {
                    success = true,
                    message = "Product succesvol gescand",
                    aantal = product.aantal,
                    colli = product.colli,
                    isComplete = isComplete,
                    allProductsScanned = allProductsScanned
                });
            }
            catch (Exception ex)
            {
                // Log and return error
                Console.WriteLine($"API Exception: {ex.Message}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Reports a missing product, creating a record in the database
        /// </summary>
        /// <param name="report">The missing product report details</param>
        /// <returns>Result of the report operation</returns>
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
                    Amount = report.Amount,
                    ReportedAt = DateTime.Now,
                    ReportedBy = User.Identity.Name
                };
                
                // Add to database
                _context.MissingProductReports.Add(missingReport);
                
                // Update the product's gemeld status
                var product = await _context.Products
                    .FirstOrDefaultAsync(p => p.orderregelnummer == report.OrderregelNummer);
                
                bool allProductsScanned = false;
                bool isComplete = false;
                if (product != null)
                {
                    // Update the reported count for this product
                    product.gemeld = report.Amount;
                    
                    // Check if all products for this vehicle are scanned or reported
                    if (!string.IsNullOrEmpty(product.voertuig))
                    {
                        // Get all products for this vehicle
                        var vehicleProducts = await _context.Products
                            .Where(p => p.voertuig == product.voertuig)
                            .ToListAsync();
                        
                        isComplete = product.aantal + product.gemeld == product.colli;
                        if (isComplete)
                        {
                            product.gescand = true;
                        }
                        
                        // A product is considered processed if it's either scanned or reported as missing
                        // Note: We include the current product in the check even if it's not yet saved
                        allProductsScanned = vehicleProducts.All(p => p.gescand || p.orderregelnummer == report.OrderregelNummer);
                        
                        if (allProductsScanned)
                        {
                            // Log that all products are processed
                            Console.WriteLine($"Alle producten voor {product.voertuig} zijn gescand of gemeld.");
                        }
                    }
                }
                
                // Save all changes to the database
                await _context.SaveChangesAsync();
               
                // Return success response with additional information
                return Ok(new { success = true, allProductsScanned = allProductsScanned, aantalGemeld = report.Amount, isComplete = isComplete });
            }
            catch (Exception ex)
            {
                // Return error response
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }
        
        /// <summary>
        /// Request model for barcode scanning
        /// </summary>
        public class ScanRequest
        {
            /// <summary>
            /// The barcode to process
            /// </summary>
            public string Barcode { get; set; }
        }
        
        /// <summary>
        /// Request model for reporting missing products
        /// </summary>
        public class MissingProductReport
        {
            /// <summary>
            /// The order line number of the missing product
            /// </summary>
            public string OrderregelNummer { get; set; }
            
            /// <summary>
            /// The description of the missing product
            /// </summary>
            public string Artikelomschrijving { get; set; }
            
            /// <summary>
            /// The reason why the product is missing
            /// </summary>
            public string Reason { get; set; }
            
            /// <summary>
            /// Additional comments about the missing product
            /// </summary>
            public string Comments { get; set; }
            
            /// <summary>
            /// The number of missing items
            /// </summary>
            public int Amount {get; set;}
        }
    }
}
