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
    [Authorize(Roles = "Laadploeg, Admin")]
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
        /// Verwerkt een barcodescan en werkt het productaantal in de database bij.
        /// </summary>
        /// <param name="request">Het scanverzoek met de barcode en voertuig ID.</param>
        /// <returns>Resultaat van de scanoperatie met bijgewerkte statusinformatie.</returns>
        [HttpPost("ProcessBarcode")]
        public async Task<IActionResult> ProcessBarcode([FromBody] ScanRequest request)
        {
            if (request?.Barcode == null || string.IsNullOrEmpty(request.VehicleId))
            {
                _logger.LogWarning("Ongeldig scanverzoek ontvangen (Barcode of VoertuigId ontbreekt)");
                return BadRequest(new { success = false, message = "Barcode en Voertuig ID zijn verplicht." });
            }

            _logger.LogInformation("Verwerken barcode: {Barcode} voor Voertuig: {VehicleId}", request.Barcode, request.VehicleId);

            try
            {
                var product = await _context.Orders
                    .FirstOrDefaultAsync(p => p.orderregelnummer == request.Barcode && p.voertuig == request.VehicleId)
                    .ConfigureAwait(false);

                if (product == null)
                {
                    _logger.LogWarning("Product niet gevonden voor barcode: {Barcode} en voertuig: {VehicleId}", request.Barcode, request.VehicleId);
                    return NotFound(new { success = false, message = "Product niet gevonden voor dit voertuig." });
                }

                if (IsOrderCompleet(product))
                {
                    _logger.LogInformation("Barcode {Barcode} is al volledig verwerkt.", request.Barcode);
                    var (huidigVerwerkt, totaalAantal) = await GetVehicleScanCounts(request.VehicleId);
                    bool isKlantFaseCompleet = await CheckKlantFaseCompleet(request.VehicleId, product.klantnaam, product.artikelomschrijving);
                    bool isVoertuigModusCompleet = await CheckVoertuigModusCompleet(request.VehicleId, product.artikelomschrijving);
                    bool isVoertuigVolledigCompleet = await CheckVoertuigVolledigCompleet(request.VehicleId);

                    return Ok(new
                    {
                        success = true,
                        message = "Product al volledig verwerkt.",
                        orderRegelNummer = product.orderregelnummer,
                        newCount = product.aantal,
                        newGemeld = product.gemeld,
                        isOrderCompleet = true,
                        vehicleScannedCount = huidigVerwerkt,
                        vehicleTotalCount = totaalAantal,
                        isKlantFaseCompleet = isKlantFaseCompleet,
                        isVoertuigModusCompleet = isVoertuigModusCompleet,
                        isVoertuigVolledigCompleet = isVoertuigVolledigCompleet
                    });
                }

                // Transaction logic for updating the product count
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    int huidigAantal = 0;
                    int colliWaarde = 0;
                    bool colliParseSuccess = int.TryParse(product.colli, out colliWaarde);

                    if (int.TryParse(product.aantal, out huidigAantal))
                    {
                        huidigAantal++;
                        product.aantal = huidigAantal.ToString();
                    }
                    else
                    {
                        _logger.LogWarning("Kon 'aantal' ('{Aantal}') niet parsen voor barcode {Barcode}. Starten met 1.", product.aantal, request.Barcode);
                        huidigAantal = 1;
                        product.aantal = "1";
                    }

                    bool isOrderNuCompleet = colliParseSuccess && huidigAantal >= colliWaarde - product.gemeld;
                    product.gescand = isOrderNuCompleet;

                    _context.Orders.Update(product);
                    await _context.SaveChangesAsync();

                    var (bijgewerktVerwerkt, totaalAantalOrders) = await GetVehicleScanCounts(request.VehicleId);
                    bool isKlantFaseNuCompleet = await CheckKlantFaseCompleet(request.VehicleId, product.klantnaam, product.artikelomschrijving);
                    bool isVoertuigModusNuCompleet = await CheckVoertuigModusCompleet(request.VehicleId, product.artikelomschrijving);
                    bool isVoertuigNuVolledigCompleet = await CheckVoertuigVolledigCompleet(request.VehicleId);

                    await transaction.CommitAsync();
                    _logger.LogInformation("Barcode {Barcode} succesvol verwerkt. Nieuw aantal: {NewCount}. Order compleet: {IsOrderComplete}", request.Barcode, product.aantal, isOrderNuCompleet);

                    return Ok(new
                    {
                        success = true,
                        message = "Barcode succesvol verwerkt.",
                        orderRegelNummer = product.orderregelnummer,
                        newCount = product.aantal,
                        newGemeld = product.gemeld,
                        isOrderCompleet = isOrderNuCompleet,
                        vehicleScannedCount = bijgewerktVerwerkt,
                        vehicleTotalCount = totaalAantalOrders,
                        isKlantFaseCompleet = isKlantFaseNuCompleet,
                        isVoertuigModusCompleet = isVoertuigModusNuCompleet,
                        isVoertuigVolledigCompleet = isVoertuigNuVolledigCompleet
                    });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Fout tijdens transactie voor barcode {Barcode}", request.Barcode);
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Algemene fout bij verwerken barcode {Barcode}", request.Barcode);
                return StatusCode(500, new { success = false, message = "Interne serverfout bij verwerken scan." });
            }
        }

        /// <summary>
        /// Verwerkt een melding van een ontbrekend product.
        /// </summary>
        /// <param name="request">Het meldingsverzoek met orderregelnummer, reden, etc.</param>
        /// <returns>Resultaat van de meldingsoperatie met bijgewerkte statusinformatie.</returns>
        [HttpPost("ReportMissing")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReportMissing([FromForm] ReportMissingRequest request)
        {
            // Controleer of het model zelf geldig is (exclusief Comments als [Required] weg is)
            if (!ModelState.IsValid)
            {
                // Als je [Required] op Comments laat staan, komt de fout hier al.
                // Als je [Required] weghaalt, controleert dit de andere [Required] velden.
                 _logger.LogWarning("Ongeldig meldingsverzoek ontvangen (ModelState): {@ModelState}", ModelState);
                 // Stuur de ModelState errors terug naar de client voor betere feedback
                 return BadRequest(new { success = false, message = "Validatiefouten.", errors = ModelState });
            }

            // Zet null comments om naar een lege string (of een andere standaardwaarde)
            // Dit gebeurt *na* de ModelState validatie.
        
            _logger.LogInformation("Verwerken melding voor Orderregel: {Orderregelnummer}, Aantal: {Amount}, Reden: {Reason}, Opmerkingen: '{Comments}'",
                request.Orderregelnummer, request.Amount, request.Reason, request.Comments);

            try
            {
                var product = await _context.Orders.FirstOrDefaultAsync(o => o.orderregelnummer == request.Orderregelnummer);
                if (product == null)
                {
                    return NotFound(new { success = false, message = "Order niet gevonden." });
                }

                // Update gemeld aantal met het opgegeven aantal
                product.gemeld += request.Amount;
                
                _context.Orders.Update(product);
                await _context.SaveChangesAsync();

                // Haal de bijgewerkte tellingen op
                var (bijgewerktVerwerkt, totaalAantalOrders) = await GetVehicleScanCounts(product.voertuig);

                // Stuur JSON response terug voor handleApiResponse
                return Ok(new
                {
                    success = true,
                    message = $"{request.Amount} product(en) succesvol gemeld.",
                    orderRegelNummer = product.orderregelnummer,
                    newCount = product.aantal,
                    newGemeld = product.gemeld,
                    isOrderCompleet = IsOrderCompleet(product),
                    vehicleScannedCount = bijgewerktVerwerkt,
                    vehicleTotalCount = totaalAantalOrders
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fout bij verwerken melding voor {Orderregelnummer}, aantal: {Amount}", 
                    request.Orderregelnummer, request.Amount);
                return StatusCode(500, new { success = false, message = "Er is een fout opgetreden bij het verwerken van de melding." });
            }
        }

        /// <summary>
        /// Verwerkt een verzoek om het aantal gescande items voor een product te verminderen.
        /// Deze actie zal nooit een klantfase of voertuig als compleet markeren.
        /// </summary>
        /// <param name="request">Het verzoek met de barcode en voertuig ID.</param>
        /// <returns>Resultaat van de operatie met bijgewerkte statusinformatie voor de specifieke order.</returns>
        [HttpPost("DecrementScan")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DecrementScan([FromBody] ScanRequest request)
        {
            if (request?.Barcode == null || string.IsNullOrEmpty(request.VehicleId))
            {
                _logger.LogWarning("Ongeldig decrement verzoek ontvangen (Barcode of VoertuigId ontbreekt)");
                return BadRequest(new { success = false, message = "Barcode en Voertuig ID zijn verplicht." });
            }

            _logger.LogInformation("Verwerken decrement scan voor barcode: {Barcode} voor Voertuig: {VehicleId}", 
                request.Barcode, request.VehicleId);

            try
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var product = await _context.Orders
                        .FirstOrDefaultAsync(p => p.orderregelnummer == request.Barcode && p.voertuig == request.VehicleId);

                    if (product == null)
                    {
                        _logger.LogWarning("Product niet gevonden voor decrement: {Barcode} en voertuig: {VehicleId}", 
                            request.Barcode, request.VehicleId);
                        return NotFound(new { success = false, message = "Product niet gevonden voor dit voertuig." });
                    }

                    // Parse huidige aantal
                    if (!int.TryParse(product.aantal, out int huidigAantal))
                    {
                        _logger.LogWarning("Kon 'aantal' ('{Aantal}') niet parsen voor barcode {Barcode} bij decrement.", 
                            product.aantal, request.Barcode);
                        return BadRequest(new { success = false, message = "Huidig aantal gescand is ongeldig." });
                    }

                    if (huidigAantal <= 0)
                    {
                        _logger.LogInformation("Poging tot decrement op barcode {Barcode} met aantal {Aantal}. Geen actie ondernomen.", 
                            request.Barcode, huidigAantal);
                        
                        var (scanCount, totalCount) = await GetVehicleScanCounts(request.VehicleId);
                        return Ok(new
                        {
                            success = true,
                            message = "Aantal was al 0, kan niet verminderen.",
                            orderRegelNummer = product.orderregelnummer,
                            newCount = product.aantal,
                            newGemeld = product.gemeld,
                            isOrderCompleet = IsOrderCompleet(product),
                            vehicleScannedCount = scanCount,
                            vehicleTotalCount = totalCount
                        });
                    }

                    // Verminder aantal
                    huidigAantal--;
                    product.aantal = huidigAantal.ToString();

                    // Update gescand status
                    product.gescand = IsOrderCompleet(product);

                    _context.Orders.Update(product);
                    await _context.SaveChangesAsync();

                    var (bijgewerktVerwerkt, totaalAantalOrders) = await GetVehicleScanCounts(request.VehicleId);

                    await transaction.CommitAsync();

                    _logger.LogInformation("Barcode {Barcode} succesvol gedecrementeerd. Nieuw aantal: {NewCount}", 
                        request.Barcode, product.aantal);

                    return Ok(new
                    {
                        success = true,
                        message = "Scan succesvol verminderd.",
                        orderRegelNummer = product.orderregelnummer,
                        newCount = product.aantal,
                        newGemeld = product.gemeld,
                        isOrderCompleet = IsOrderCompleet(product),
                        vehicleScannedCount = bijgewerktVerwerkt,
                        vehicleTotalCount = totaalAantalOrders
                    });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Fout tijdens transactie voor decrement barcode {Barcode}", request.Barcode);
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Algemene fout bij verwerken decrement barcode {Barcode}", request.Barcode);
                return StatusCode(500, new { success = false, message = "Interne serverfout bij verwerken decrement." });
            }
        }

        /// <summary>
        /// Request model for scanning a barcode.
        /// </summary>
        public class ScanRequest
        {
            public string Barcode { get; set; }
            public string VehicleId { get; set; }
        }

        /// <summary>
        /// Request model for reporting a missing product.
        /// Properties should match the form field names.
        /// </summary>
        public class ReportMissingRequest
        {
            public string Orderregelnummer { get; set; }
            public string Artikelomschrijving { get; set; } // Niet strikt nodig voor backend logic, maar kan handig zijn voor logging
            public string Reason { get; set; }
            public string Comments { get; set; }
            public int Amount { get; set; }
            // Voeg __RequestVerificationToken toe als je model binding gebruikt voor het token,
            // anders wordt het meestal via headers of form data direct afgehandeld.
            // public string __RequestVerificationToken { get; set; }

            // Voeg VehicleId toe als het nodig is om het product uniek te identificeren
            // public string VehicleId { get; set; }
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
            var vehicleProducts = await _context.Orders
                .Where(p => p.voertuig == vehicleId)
                .ToListAsync();

            bool allScanned = vehicleProducts.All(p => p.gescand);

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
            // Get current user's ShowAll setting
            var currentUser = await _context.Users
                .FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);
            bool showAllProducts = currentUser?.ShowAll ?? false;

            if (showAllProducts)
            {
                // In ShowAll mode, we only check if all products of the current type are scanned
                var heavyProductNames = await _context.HeavyProducts
                    .Select(hp => hp.Name)
                    .ToListAsync();

                var allProducts = await _context.Orders
                    .Where(p => p.voertuig == vehicleId)
                    .ToListAsync();

                var heavyProducts = allProducts
                    .Where(p => heavyProductNames.Any(hp => 
                        p.artikelomschrijving != null && 
                        p.artikelomschrijving.Contains(hp)))
                    .ToList();

                var regularProducts = allProducts
                    .Where(p => !heavyProductNames.Any(hp => 
                        p.artikelomschrijving != null && 
                        p.artikelomschrijving.Contains(hp)))
                    .ToList();

                // Return true if all products of the current type are scanned
                return regularProducts.Any(p => p.gescand) ? 
                    regularProducts.All(p => p.gescand) : 
                    heavyProducts.All(p => p.gescand);
            }
            else
            {
                // Original customer-by-customer logic
                return await CheckAllCustomerProductsScannedOriginal(vehicleId, customerName);
            }
        }

        private async Task<bool> CheckAllCustomerProductsScannedOriginal(string vehicleId, string customerName)
        {
            // Get all heavy product names from the database
            var heavyProductNames = await _context.HeavyProducts
                .Select(hp => hp.Name)
                .ToListAsync();

            // Single database call to get all relevant products
            var allProducts = await _context.Orders
                .Where(p => p.voertuig == vehicleId)
                .ToListAsync();

            // Filter in memory
            var customerHeavyProducts = allProducts
                .Where(p => p.klantnaam == customerName)
                .Where(p => heavyProductNames.Any(hp => 
                    p.artikelomschrijving != null && 
                    p.artikelomschrijving.Contains(hp)))
                .ToList();

            var customerRegularProducts = allProducts
                .Where(p => p.klantnaam == customerName)
                .Where(p => !heavyProductNames.Any(hp => 
                    p.artikelomschrijving != null && 
                    p.artikelomschrijving.Contains(hp)))
                .ToList();

            // If we're scanning heavy products (customer has heavy products)
            if (customerHeavyProducts.Any())
            {
                return customerHeavyProducts.All(p => p.gescand);
            }
            
            // If we're scanning regular products
            if (customerRegularProducts.Any())
            {
                return customerRegularProducts.All(p => p.gescand);
            }

            return false;
        }

        /// <summary>
        /// Haalt het aantal verwerkte (gescand + gemeld) orders en het totaal aantal orders voor een voertuig op.
        /// </summary>
        /// <param name="vehicleId">Het ID van het voertuig.</param>
        /// <returns>Een tuple met het aantal verwerkte orders en het totaal aantal orders.</returns>
        private async Task<(int processedCount, int totalCount)> GetVehicleScanCounts(string vehicleId)
        {
            if (string.IsNullOrEmpty(vehicleId))
            {
                return (0, 0);
            }

            // Haal alle orders voor het voertuig in één keer op
            var vehicleOrders = await _context.Orders
                .Where(o => o.voertuig == vehicleId)
                .ToListAsync(); // Gebruik ToListAsync om de query uit te voeren

            var totalCount = vehicleOrders.Count;

            // Tel het aantal orders dat als compleet wordt beschouwd (gescand + gemeld >= colli)
            // door de bestaande IsOrderCompleet helper methode te hergebruiken.
            var processedCount = vehicleOrders.Count(IsOrderCompleet);

            return (processedCount, totalCount);
        }

        /// <summary>
        /// Controleert of een order compleet is (aantal gescand + aantal gemeld >= colli).
        /// </summary>
        /// <param name="order">De order om te controleren.</param>
        /// <returns>True als de order compleet is, anders false.</returns>
        private bool IsOrderCompleet(Order order)
        {
            if (order == null) return false;

            // Deze logica houdt al rekening met 'gemeld'.
            // aantal >= colli - gemeld  is hetzelfde als  aantal + gemeld >= colli
            if (int.TryParse(order.colli, out int colli) && int.TryParse(order.aantal, out int aantal))
            {
                // Zorg ervoor dat colli groter dan 0 is om deling door nul of onlogische voltooiing te voorkomen
                return colli > 0 && (aantal + order.gemeld) >= colli;
            }

            _logger.LogWarning("Kon colli ('{Colli}') of aantal ('{Aantal}') niet parsen voor order {OrderRegel} bij IsOrderCompleet check.", order.colli, order.aantal, order.orderregelnummer);
            return false; // Als parsen mislukt, beschouw het als niet compleet.
        }

        /// <summary>
        /// Controleert of alle producten voor de *huidige fase* (schermen of gewoon)
        /// van een specifieke klant op een voertuig compleet zijn.
        /// </summary>
        /// <param name="vehicleId">Voertuig ID</param>
        /// <param name="customerName">Klantnaam</param>
        /// <param name="scannedArtikelOmschrijving">Artikelomschrijving van het *zojuist gescande* product om de fase te bepalen.</param>
        /// <returns>True als de fase compleet is.</returns>
        private async Task<bool> CheckKlantFaseCompleet(string vehicleId, string customerName, string scannedArtikelOmschrijving)
        {
            if (string.IsNullOrEmpty(vehicleId) || string.IsNullOrEmpty(customerName)) return false;

            var heavyProductNamesLower = await _context.HeavyProducts.Select(hp => hp.Name.ToLower()).ToListAsync();
            bool isScannedProductHeavy = heavyProductNamesLower.Any(hp => scannedArtikelOmschrijving?.ToLower().Contains(hp) ?? false);

            var customerOrders = await _context.Orders
                .Where(o => o.voertuig == vehicleId && o.klantnaam == customerName)
                .ToListAsync();

            if (!customerOrders.Any()) return true; // Geen orders voor klant, dus fase is "compleet"

            IEnumerable<Order> ordersInFase;
            if (isScannedProductHeavy)
            {
                // Controleer alle schermen voor deze klant
                ordersInFase = customerOrders.Where(o => heavyProductNamesLower.Any(hp => o.artikelomschrijving?.ToLower().Contains(hp) ?? false));
            }
            else
            {
                // Controleer alle gewone producten voor deze klant
                ordersInFase = customerOrders.Where(o => !heavyProductNamesLower.Any(hp => o.artikelomschrijving?.ToLower().Contains(hp) ?? false));
            }

            // Als er geen orders in deze fase zijn, is de fase technisch compleet.
            // Controleer of *alle* orders in deze specifieke fase compleet zijn.
            return !ordersInFase.Any() || ordersInFase.All(IsOrderCompleet);
        }


        /// <summary>
        /// Controleert of alle producten van een specifieke *modus* (alle schermen of alle gewone)
        /// voor een voertuig compleet zijn.
        /// </summary>
        /// <param name="vehicleId">Voertuig ID</param>
        /// <param name="scannedArtikelOmschrijving">Artikelomschrijving van het *zojuist gescande* product om de modus te bepalen.</param>
        /// <returns>True als de modus compleet is.</returns>
        private async Task<bool> CheckVoertuigModusCompleet(string vehicleId, string scannedArtikelOmschrijving)
        {
             if (string.IsNullOrEmpty(vehicleId)) return false;

            var heavyProductNamesLower = await _context.HeavyProducts.Select(hp => hp.Name.ToLower()).ToListAsync();
            bool isScannedProductHeavy = heavyProductNamesLower.Any(hp => scannedArtikelOmschrijving?.ToLower().Contains(hp) ?? false);

            var allVehicleOrders = await _context.Orders
                .Where(o => o.voertuig == vehicleId)
                .ToListAsync();

            if (!allVehicleOrders.Any()) return true; // Geen orders, dus modus is "compleet"

            IEnumerable<Order> ordersInModus;
            if (isScannedProductHeavy)
            {
                // Controleer alle schermen voor dit voertuig
                ordersInModus = allVehicleOrders.Where(o => heavyProductNamesLower.Any(hp => o.artikelomschrijving?.ToLower().Contains(hp) ?? false));
            }
            else
            {
                // Controleer alle gewone producten voor dit voertuig
                ordersInModus = allVehicleOrders.Where(o => !heavyProductNamesLower.Any(hp => o.artikelomschrijving?.ToLower().Contains(hp) ?? false));
            }

            // Als er geen orders in deze modus zijn, is de modus technisch compleet.
            // Controleer of *alle* orders in deze specifieke modus compleet zijn.
            return !ordersInModus.Any() || ordersInModus.All(IsOrderCompleet);
        }

        /// <summary>
        /// Controleert of *alle* producten (zowel schermen als gewoon) voor een voertuig compleet zijn.
        /// </summary>
        /// <param name="vehicleId">Voertuig ID</param>
        /// <returns>True als alle producten compleet zijn.</returns>
        private async Task<bool> CheckVoertuigVolledigCompleet(string vehicleId)
        {
            if (string.IsNullOrEmpty(vehicleId)) return false;

            var allVehicleOrders = await _context.Orders
                .Where(o => o.voertuig == vehicleId)
                .ToListAsync();

            if (!allVehicleOrders.Any()) return true; // Geen orders, dus voertuig is "compleet"

            // Controleer of *alle* orders compleet zijn
            return allVehicleOrders.All(IsOrderCompleet);
        }
    }
}
