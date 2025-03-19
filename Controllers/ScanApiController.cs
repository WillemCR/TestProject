using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch.Internal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using TestProject.Data;
using TestProject.Models;
using Microsoft.Extensions.Logging;

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
        private readonly ILogger<ScanApiController> _logger;

        /// <summary>
        /// Constructor that injects the database context and logger
        /// </summary>
        /// <param name="context">The application database context</param>
        /// <param name="logger">The logger for this controller</param>
        public ScanApiController(ApplicationDbContext context, ILogger<ScanApiController> logger)
        {
            _context = context;
            _logger = logger;
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
                _logger.LogInformation("ProcessBarcode API endpoint bereikt");

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
                if (!(product.colli > product.aantal + product.gemeld))
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
                    allProductsScanned = await CheckAllVehicleProductsScanned(product.voertuig);
                }
                
                bool allCustomerProductsScanned = false;
                if (isComplete && !string.IsNullOrEmpty(product.klantnaam))
                {
                    allCustomerProductsScanned = await CheckAllCustomerProductsScanned(product.voertuig, product.klantnaam);
                }

               
                // Return success response with additional information
                return Ok(new
                {
                    success = true,
                    message = "Product succesvol gescand",
                    aantal = product.aantal,
                    colli = product.colli,
                    isComplete = isComplete,
                    allProductsScanned = allProductsScanned,
                    allCustomerProductsScanned = allCustomerProductsScanned
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API Exception");
                return StatusCode(500, new { success = false, message = "Er is een fout opgetreden" });
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
                _logger.LogInformation("ReportMissing API endpoint bereikt");

                // Get the product first to validate the amount
                var product = await _context.Products
                    .FirstOrDefaultAsync(p => p.orderregelnummer == report.OrderregelNummer);

                if (product == null)
                {
                    return NotFound(new { success = false, message = "Product niet gevonden" });
                }

                // Calculate remaining amount that can be reported
                int remainingAmount = product.colli - product.aantal - product.gemeld;

                // Validate the reported amount
                if (report.Amount > remainingAmount)
                {
                    return BadRequest(new { 
                        success = false, 
                        message = $"U kunt maximaal {remainingAmount} producten melden als ontbrekend" 
                    });
                }

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
                product.gemeld = report.Amount;

                // Check if all products for this vehicle are scanned or reported
                bool allProductsScanned = false;
                bool isComplete = false;
                if (!string.IsNullOrEmpty(product.voertuig))
                {
                    isComplete = product.aantal + product.gemeld == product.colli;
                    if (isComplete)
                    {
                        product.gescand = true;
                    }

                    // Check if all products for this vehicle are scanned or reported
                    allProductsScanned = await CheckAllVehicleProductsScanned(product.voertuig, report.OrderregelNummer);
                }

                // Save all changes to the database
                await _context.SaveChangesAsync();
                bool allCustomerProductsScanned = false;
                if (isComplete && !string.IsNullOrEmpty(product.klantnaam))
                {
                    allCustomerProductsScanned = await CheckAllCustomerProductsScanned(product.voertuig, product.klantnaam);
                }

  
                // Return success response with additional information
                return Ok(new { success = true,  allCustomerProductsScanned = allCustomerProductsScanned, allProductsScanned = allProductsScanned, aantalGemeld = report.Amount, isComplete = isComplete });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reporting missing product");
                return StatusCode(500, new { success = false, message = "Er is een fout opgetreden bij het melden van het ontbrekende product" });
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
            public int Amount { get; set; }
        }

        /// <summary>
        /// Checks if all products for a specific vehicle are scanned or reported
        /// </summary>
        /// <param name="vehicleId">The vehicle ID to check</param>
        /// <param name="currentOrderNumber">Optional order number for ReportMissing case</param>
        /// <returns>True if all products are scanned or reported</returns>
        private async Task<bool> CheckAllVehicleProductsScanned(string vehicleId, string currentOrderNumber = null)
        {
            // Get all products for this vehicle
            var vehicleProducts = await _context.Products
                .Where(p => p.voertuig == vehicleId)
                .ToListAsync();

            bool allScanned;
            
            // If we have a current order number (for ReportMissing case), use that in the check
            if (!string.IsNullOrEmpty(currentOrderNumber))
            {
                allScanned = vehicleProducts.All(p => p.gescand || p.orderregelnummer == currentOrderNumber);
            }
            else
            {
                // For ProcessBarcode case
                allScanned = vehicleProducts.All(p => p.gescand || p.gemeld > 0);
            }

            if (allScanned)
            {
                Console.WriteLine($"All products for vehicle {vehicleId} are scanned or reported.");
            }
            
            return allScanned;
        }

        /// <summary>
        /// Checks if all products for a specific customer and vehicle are scanned or reported
        /// </summary>
        /// <param name="vehicleId">The vehicle ID to check</param>
        /// <param name="customerName">The customer name to check</param>
        /// <returns>True if all products are scanned or reported</returns>
        private async Task<bool> CheckAllCustomerProductsScanned(string vehicleId, string customerName)
        {
            // Get all products for this customer and vehicle
            var customerProducts = await _context.Products
                .Where(p => p.voertuig == vehicleId && p.klantnaam == customerName)
                .ToListAsync();

            // Check if all products for this customer are processed
            return customerProducts.All(p => p.gescand || p.gemeld > 0);
        }
    }
}
