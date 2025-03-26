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
                _logger.LogInformation("Processing barcode: {Barcode}", request?.Barcode ?? "null");

                // Validate the request
                if (request?.Barcode == null)
                {
                    _logger.LogWarning("Invalid barcode request received");
                    return BadRequest(new { success = false, message = "Barcode is verplicht" });
                }

                // Use async/await properly with ConfigureAwait
                var product = await _context.Products
                    .FirstOrDefaultAsync(p => p.orderregelnummer == request.Barcode)
                    .ConfigureAwait(false);

                if (product == null)
                {
                    _logger.LogWarning("Product not found for barcode: {Barcode}", request.Barcode);
                    return NotFound(new { success = false, message = "Product niet gevonden" });
                }

                // Guard against integer overflow
                if (product.aantal >= int.MaxValue - 1)
                {
                    _logger.LogError("Product count would overflow: {Barcode}", request.Barcode);
                    return BadRequest(new { success = false, message = "Maximum aantal overschreden" });
                }

                // Check remaining capacity
                var remaining = product.colli - (product.aantal + product.gemeld);
                if (remaining <= 0)
                {
                    _logger.LogWarning("Maximum product count reached for: {Barcode}", request.Barcode);
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
                _logger.LogError(ex, "Unexpected error processing barcode: {Barcode}", request?.Barcode ?? "null");
                return StatusCode(500, new { success = false, message = "Er is een onverwachte fout opgetreden" });
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
                _logger.LogInformation("Processing missing product report for: {OrderNumber}", report?.OrderregelNummer ?? "null");

                if (report == null || string.IsNullOrEmpty(report.OrderregelNummer))
                {
                    _logger.LogWarning("Invalid missing product report received");
                    return BadRequest(new { success = false, message = "Ongeldige melding" });
                }

                if (report.Amount <= 0)
                {
                    return BadRequest(new { success = false, message = "Aantal moet groter zijn dan 0" });
                }

                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var product = await _context.Products
                        .FirstOrDefaultAsync(p => p.orderregelnummer == report.OrderregelNummer)
                        .ConfigureAwait(false);

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

                    // Create missing report with sanitized input
                    var missingReport = new MissingProductReportEntity
                    {
                        OrderregelNummer = report.OrderregelNummer,
                        Artikelomschrijving = report.Artikelomschrijving?.Trim(),
                        Reason = report.Reason?.Trim(),
                        Comments = report.Comments?.Trim() ?? "Geen opmerkingen",
                        Amount = report.Amount,
                        ReportedAt = DateTime.UtcNow,
                        ReportedBy = User.Identity.Name,
                    };

                    _context.MissingProductReports.Add(missingReport);
                    
                    // Update product status
                    product.gemeld = report.Amount;
                    if (product.aantal + product.gemeld == product.colli)
                    {
                        product.gescand = true;
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

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
                        allProductsScanned = await CheckAllVehicleProductsScanned(product.voertuig);
                    }

                    bool allCustomerProductsScanned = false;
                    if (isComplete && !string.IsNullOrEmpty(product.klantnaam))
                    {
                        allCustomerProductsScanned = await CheckAllCustomerProductsScanned(product.voertuig, product.klantnaam);
                    }

                    // Return success response with additional information
                    return Ok(new { success = true,  allCustomerProductsScanned = allCustomerProductsScanned, allProductsScanned = allProductsScanned, aantalGemeld = report.Amount, isComplete = isComplete });
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing missing product report for: {OrderNumber}", report?.OrderregelNummer ?? "null");
                return StatusCode(500, new { success = false, message = "Er is een onverwachte fout opgetreden" });
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
        private async Task<bool> CheckAllVehicleProductsScanned(string vehicleId)
        {
            // Get all products for this vehicle
            var vehicleProducts = await _context.Products
                .Where(p => p.voertuig == vehicleId)
                .ToListAsync();

            bool allScanned;
           
             allScanned = vehicleProducts.All(p => p.gescand);
           

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
            // Get all heavy product names from the database
            var heavyProductNames = await _context.HeavyProducts
                .Select(hp => hp.Name)
                .ToListAsync();

            // Get all products for this customer and vehicle
            var customerProducts = await _context.Products
                .Where(p => p.voertuig == vehicleId && p.klantnaam == customerName)
                .ToListAsync();

            // Separate heavy and regular products
            var heavyProducts = customerProducts
                .Where(p => heavyProductNames.Any(hp => 
                    p.artikelomschrijving != null && 
                    p.artikelomschrijving.Contains(hp)))
                .ToList();

            var regularProducts = customerProducts
                .Where(p => !heavyProductNames.Any(hp => 
                    p.artikelomschrijving != null && 
                    p.artikelomschrijving.Contains(hp)))
                .ToList();

            // Check if there are any heavy products
            if (heavyProducts.Any())
            {
                // If there are heavy products, they must all be scanned
                return heavyProducts.All(p => p.gescand);
            }

            // If no heavy products, check regular products
            // Only consider regular products complete if all heavy products for ALL customers are scanned
            var allVehicleHeavyProducts = await _context.Products
                .Where(p => p.voertuig == vehicleId)
                .Where(p => heavyProductNames.Any(hp => 
                    p.artikelomschrijving != null && 
                    p.artikelomschrijving.Contains(hp)))
                .ToListAsync();

            if (allVehicleHeavyProducts.Any() && !allVehicleHeavyProducts.All(p => p.gescand))
            {
                // If there are unscanned heavy products for any customer, return false
                return false;
            }

            // All heavy products are scanned, now check regular products for this customer
            return regularProducts.All(p => p.gescand);
        }
    }
}
