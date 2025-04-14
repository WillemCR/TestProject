using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TestProject.Data;
using TestProject.Models;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Identity; // Nodig voor User

namespace TestProject.Pages
{
    /// <summary>
    /// Page model for the product scanning interface.
    /// Requires user to be in Laadploeg, Planner, or Admin role.
    /// </summary>
    [Authorize(Roles = "Laadploeg, Admin")]
    public class ScanModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ScanModel> _logger;

        /// <summary>
        /// Constructor that injects the database context and logger
        /// </summary>
        /// <param name="context">The application database context</param>
        /// <param name="logger">The logger for this page</param>
        public ScanModel(ApplicationDbContext context, ILogger<ScanModel> logger)
        {
            _context = context;
            _logger = logger;
            _logger.LogInformation("ScanModel constructor called.");
            // Initialize collections in constructor
            VehicleStatuses = new List<VehicleStatus>();
            ProductsToScan = new List<Order>();
            LosseArtikelenPerOrder = new Dictionary<string, List<LosseArtikelen>>();
        }

        /// <summary>
        /// List of all unique vehicle identifiers in the system
        /// </summary>
        public List<VehicleStatus> VehicleStatuses { get; set; } = new();

        /// <summary>
        /// Currently selected vehicle identifier
        /// </summary>
        [BindProperty(SupportsGet = true)]
        public string? SelectedVehicle { get; set; }

        /// <summary>
        /// Currently selected customer name
        /// </summary>
        [BindProperty(SupportsGet = true)]
        public string? SelectedCustomer { get; set; }

        /// <summary>
        /// Flag indicating if this page should show regular orders instead of heavy orders
        /// </summary>
        [BindProperty(SupportsGet = true)]
        public string? SelectedMode { get; set; } // "heavy" or "regular"

        /// <summary>
        /// Flag indicating if this page should show regular products instead of heavy orders
        /// </summary>
        public bool ShowRegularProductsMode { get; set; }

        /// <summary>
        /// List of all unique customers ordered by name
        /// </summary>
        public List<string> CustomerList { get; set; } = new List<string>();

        /// <summary>
        /// Next customer to display
        /// </summary>
        public string? NextCustomerId { get; private set; }

        /// <summary>
        /// Add new property to store loose articles
        /// </summary>
        public Dictionary<string, List<LosseArtikelen>> LosseArtikelenPerOrder { get; set; } = new Dictionary<string, List<LosseArtikelen>>();

        /// <summary>
        /// The current user
        /// </summary>
        public User? CurrentUser { get; set; }

        // --- New Properties for Button Logic ---
        public bool ShowNextHeavyCustomerButton { get; private set; }
        public bool ShowGoToRegularButton { get; private set; }
        public bool ShowNextRegularCustomerButton { get; private set; }
        public bool ShowAllCompleteMessage { get; private set; }
        public bool ShowRegularOrdersCompleteForCustomer { get; private set; }// Helper for regular check
        public bool IsKlantFaseCompleet { get; private set; } = false;
        public bool IsVoertuigVolledigCompleet { get; private set; } = false;
 
   

        // Initialiseer lijsten direct om null-waarschuwingen te voorkomen
        public IList<Order> ProductsToScan { get; set; } = new List<Order>();
     
        // Maak deze public zodat de view erbij kan
        public Order? CurrentCustomer { get; private set; } // Houdt info over de huidige klant bij

        // Houdt info bij over de geselecteerde klant (eerste order)
        public Order? CurrentCustomerOrderInfo { get; private set; }

        /// <summary>
        /// Handles navigation to regular orders page
        /// </summary>
        public IActionResult OnPostShowRegularOrders(string vehicle)
        {
            _logger.LogInformation($"Redirecting to regular orders for vehicle {vehicle}");
            return RedirectToPage("Scan", new { vehicle, mode = "regular" });
        }

        /// <summary>
        /// Handles navigation to the next customer's orders or switches modes.
        /// </summary>
        /// <param name="vehicle">The selected vehicle</param>
        /// <param name="currentCustomer">The current customer</param>
        /// <param name="mode">Optional mode parameter to show regular orders</param>
        /// <returns>Redirect action to the same page with updated customer</returns>
        public async Task<IActionResult> OnPostNextCustomerAsync(string vehicle, string currentCustomer, string? mode = null)
        {
            _logger.LogInformation("OnPostNextCustomerAsync called. Vehicle: {Vehicle}, CurrentCustomer: {CurrentCustomer}, Mode: {Mode}", vehicle, currentCustomer, mode);

            if (string.IsNullOrEmpty(vehicle) || string.IsNullOrEmpty(currentCustomer))
            {
                _logger.LogWarning("OnPostNextCustomerAsync: Vehicle or CurrentCustomer is null or empty.");
                return BadRequest(new { success = false, message = "Vehicle and current customer must be provided." });
            }

            try
            {
                // Determine the mode we are currently in *before* getting the customer list
                bool wasInRegularMode = mode == "regular";
                _logger.LogInformation($"Current mode was: {(wasInRegularMode ? "regular" : "heavy")}");

                var allOrders = await _context.Orders
                    .Where(o => o.voertuig == vehicle)
                    .OrderByDescending(o => o.volgorde)
                    .ToListAsync();

                if (!allOrders.Any())
                {
                    _logger.LogWarning("OnPostNextCustomerAsync: No orders found for vehicle {Vehicle}.", vehicle);
                    // Redirect back to the start, maybe?
                    return RedirectToPage("Scan", new { vehicle });
                }

                var heavyProductNamesLower = await _context.HeavyProducts
                    .Select(hp => hp.Name.ToLower()) // Use ToLower for consistent comparison
                    .ToListAsync();

                // Get the list of customers for the mode we were just in
                var customersInCurrentMode = allOrders
                    .Where(o => wasInRegularMode
                        ? !heavyProductNamesLower.Any(hp => o.artikelomschrijving != null && o.artikelomschrijving.ToLower().Contains(hp)) // Regular product condition
                        : heavyProductNamesLower.Any(hp => o.artikelomschrijving != null && o.artikelomschrijving.ToLower().Contains(hp))) // Heavy product condition
                    .OrderByDescending(o => o.volgorde)
                    .Select(o => o.klantnaam)
                    .Distinct()
                    .ToList();

                if (!customersInCurrentMode.Any())
                {
                    _logger.LogWarning($"No customers found for vehicle {vehicle} in mode '{(wasInRegularMode ? "regular" : "heavy")}'. This shouldn't happen if the button was visible.");
                    // Redirect back to the start, maybe?
                    return RedirectToPage("Scan", new { vehicle });
                }

                _logger.LogInformation($"Customers in mode '{(wasInRegularMode ? "regular" : "heavy")}': {string.Join(", ", customersInCurrentMode)}");

                var currentIndex = customersInCurrentMode.IndexOf(currentCustomer);
                if (currentIndex == -1)
                {
                    _logger.LogWarning($"Current customer '{currentCustomer}' not found in the list for mode '{(wasInRegularMode ? "regular" : "heavy")}'. List: {string.Join(", ", customersInCurrentMode)}");
                    // Redirect back, maybe show an error?
                    return RedirectToPage("Scan", new { vehicle, customer = currentCustomer, mode }); // Stay on same page to avoid confusion
                }

                // Check if there is a next customer *in the current list*
                if (currentIndex < customersInCurrentMode.Count - 1)
                {
                    var nextCustomer = customersInCurrentMode[currentIndex + 1];
                    _logger.LogInformation($"Moving to next customer '{nextCustomer}' in mode '{(wasInRegularMode ? "regular" : "heavy")}'.");
                    // Redirect to the next customer, staying in the *same mode*
                    // Pass mode explicitly to avoid ambiguity if it was null/empty
                    return RedirectToPage("Scan", new { vehicle, customer = nextCustomer, mode = (wasInRegularMode ? "regular" : null) });
                }
                else
                {
                    // This was the last customer in the current list
                    _logger.LogInformation($"Finished last customer '{currentCustomer}' in mode '{(wasInRegularMode ? "regular" : "heavy")}'.");
                    if (!wasInRegularMode) // If we just finished the last HEAVY customer
                    {
                        // Check if there are any regular customers to switch to
                        var customersWithRegularProducts = allOrders
                           .Where(o => !heavyProductNamesLower.Any(hp => o.artikelomschrijving != null && o.artikelomschrijving.ToLower().Contains(hp)))
                           .OrderByDescending(o => o.volgorde)
                           .Select(o => o.klantnaam)
                           .Distinct()
                           .ToList();

                        if (customersWithRegularProducts.Any())
                        {
                            var firstRegularCustomer = customersWithRegularProducts.First();
                            _logger.LogInformation($"Switching to regular mode, starting with customer '{firstRegularCustomer}'.");
                            // Redirect to the first regular customer in regular mode
                            return RedirectToPage("Scan", new { vehicle, mode = "regular", customer = firstRegularCustomer });
                        }
                        else
                        {
                            _logger.LogInformation($"All heavy products scanned for vehicle {vehicle}, and no regular products found. Scan complete for vehicle.");
                            // No regular customers, vehicle is fully scanned. Redirect back to vehicle selection?
                            // For now, redirect to regular mode (which will show empty)
                             return RedirectToPage("Scan", new { vehicle, mode = "regular" });
                        }
                    }
                    else // If we just finished the last REGULAR customer
                    {
                        _logger.LogInformation($"All regular products scanned for vehicle {vehicle}. Scan complete for vehicle.");
                        // All done. Redirect back to vehicle selection or stay? Stay on the last page.
                        return RedirectToPage("Scan", new { vehicle, mode = "regular", customer = currentCustomer });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OnPostNextCustomerAsync for Vehicle: {Vehicle}, Customer: {CurrentCustomer}, Mode: {Mode}", vehicle, currentCustomer, mode);
                // Redirect back to the vehicle selection or a generic error page
                return RedirectToPage("Scan", new { vehicle });
            }
        }

        /// <summary>
        /// Handles GET requests to the page
        /// Loads vehicle data and orders based on the selected vehicle and customer
        /// </summary>
        /// <param name="vehicle">Optional vehicle identifier from query string</param>
        /// <param name="mode">Optional mode parameter to show regular orders</param>
        /// <param name="customer">Optional customer name from query string</param>
        public async Task<IActionResult> OnGetAsync(string? vehicle, string? customer = null, string? mode = null)
        {
            _logger.LogInformation("OnGetAsync called. Vehicle: {Vehicle}, Customer: {Customer}, Mode: {Mode}", vehicle, customer, mode);

            // Get current user first
            var userName = User.Identity?.Name;
            if (!string.IsNullOrEmpty(userName))
            {
                CurrentUser = await _context.Users.FirstOrDefaultAsync(u => u.UserName == userName);
                _logger.LogInformation("CurrentUser set: {UserName}, ShowAll: {ShowAll}", CurrentUser?.UserName, CurrentUser?.ShowAll);
            }
            else
            {
                 _logger.LogWarning("Could not find username from User.Identity.");
            }

            SelectedVehicle = vehicle;
            SelectedCustomer = customer;
            SelectedMode = mode ?? "heavy"; // Default to heavy mode
            _logger.LogInformation("SelectedMode set to: {SelectedMode}", SelectedMode);

            ShowRegularProductsMode = SelectedMode == "regular";

            VehicleStatuses = await GetVehicleStatusesAsync();

            if (!string.IsNullOrEmpty(SelectedVehicle))
            {
                _logger.LogInformation("Processing selected vehicle: {SelectedVehicle}", SelectedVehicle);
                var allVehicleOrders = await _context.Orders
                    .Where(o => o.voertuig == SelectedVehicle)
                    .OrderByDescending(o => o.volgorde)
                    .ToListAsync();

                if (!allVehicleOrders.Any())
                {
                    _logger.LogWarning("Geen orders gevonden voor voertuig {VehicleId}", SelectedVehicle);
                    ProductsToScan = new List<Order>(); // Ensure ProductsToScan is empty
                    return Page();
                }
                 _logger.LogInformation("Found {OrderCount} orders for vehicle {SelectedVehicle}", allVehicleOrders.Count, SelectedVehicle);

                var heavyProductNamesLower = await _context.HeavyProducts
                    .Select(hp => hp.Name.ToLower())
                    .ToListAsync();
                _logger.LogInformation("Loaded {HeavyProductCount} heavy product names.", heavyProductNamesLower.Count);

                bool hasHeavyProducts = allVehicleOrders.Any(o => heavyProductNamesLower.Any(hp => o.artikelomschrijving?.ToLower().Contains(hp) ?? false));
                bool hasRegularProducts = allVehicleOrders
                    .Where(o => !string.IsNullOrEmpty(o.artikelomschrijving))
                    .Any(o => !heavyProductNamesLower
                        .Any(hp => o.artikelomschrijving.ToLower().Contains(hp)));
                _logger.LogInformation("Vehicle {SelectedVehicle} - HasHeavyProducts: {HasHeavy}, HasRegularProducts: {HasRegular}", SelectedVehicle, hasHeavyProducts, hasRegularProducts);

                // --- Automatic Mode Switching ---
                // If heavy mode is selected BUT there are no heavy products AND there ARE regular products, switch to regular mode.
                if (SelectedMode == "heavy" && !hasHeavyProducts && hasRegularProducts)
                {
                    _logger.LogInformation("Voertuig {VehicleId} heeft geen zware producten, maar wel reguliere. Redirect naar 'regular' modus.", SelectedVehicle);
                    return RedirectToPage(new { vehicle = SelectedVehicle, customer = SelectedCustomer, mode = "regular" });
                }

                // --- Load Data ---
                await FilterProductsToScan(allVehicleOrders); // This now also calls UpdateScanStatus
                await LoadLosseArtikelen(ProductsToScan); // Load only for the products to be displayed
            }
            else
            {
                _logger.LogInformation("OnGetAsync aangeroepen zonder geselecteerd voertuig.");
                ProductsToScan = new List<Order>(); // Ensure ProductsToScan is empty
            }

            _logger.LogInformation("OnGetAsync finished.");
            return Page();
        }

        private async Task<List<VehicleStatus>> GetVehicleStatusesAsync()
        {
            _logger.LogInformation("GetVehicleStatusesAsync called.");
            var allOrders = await _context.Orders.ToListAsync();
            var statuses = allOrders
                .Where(o => !string.IsNullOrEmpty(o.voertuig))
                .GroupBy(o => o.voertuig)
                .Select(g => new VehicleStatus
                {
                    VehicleId = g.Key,
                    TotalProducts = g.Count(),
                    ScannedProducts = g.Count(IsOrderProcessed) // Use IsOrderProcessed here
                })
                .OrderBy(vs => vs.VehicleId)
                .ToList();
            _logger.LogInformation("GetVehicleStatusesAsync finished. Found {Count} vehicle statuses.", statuses.Count);
            return statuses;
        }

        private async Task LoadLosseArtikelen(IList<Order> ordersToDisplay)
        {
            _logger.LogInformation("LoadLosseArtikelen called for {OrderCount} orders.", ordersToDisplay.Count);
            if (!ordersToDisplay.Any())
            {
                LosseArtikelenPerOrder = new Dictionary<string, List<LosseArtikelen>>();
                _logger.LogInformation("No orders to display, skipping LosseArtikelen load.");
                return;
            }

            var orderRegelnummers = ordersToDisplay.Select(o => o.orderregelnummer).Distinct().ToList();
            _logger.LogInformation("Fetching LosseArtikelen for {RegelnummerCount} distinct orderregelnummers.", orderRegelnummers.Count);

            var losseArtikelen = await _context.LosseArtikelen
                .Where(la => orderRegelnummers.Contains(la.orderid))
                .ToListAsync();
            _logger.LogInformation("Found {LosseArtikelenCount} LosseArtikelen entries.", losseArtikelen.Count);

            LosseArtikelenPerOrder = losseArtikelen
                .GroupBy(la => la.orderid)
                .ToDictionary(g => g.Key, g => g.ToList());
            _logger.LogInformation("Grouped LosseArtikelen into {GroupCount} dictionary entries.", LosseArtikelenPerOrder.Count);
        }

        // Helper method to check if a single order is complete
        private bool IsOrderProcessed(Order order)
        {
            if (order == null)
            {
                _logger.LogWarning("IsOrderProcessed called with a null order.");
                return false;
            }

            bool colliParsed = int.TryParse(order.colli, out int colli);
            bool aantalParsed = int.TryParse(order.aantal, out int aantal);

            if (!colliParsed || !aantalParsed)
            {
                // Log only if parsing fails, as it might be expected for some data
                 _logger.LogTrace("Failed to parse 'colli' or 'aantal' for OrderRegel {OrderRegel}. Colli: '{Colli}', Aantal: '{Aantal}'", order.orderregelnummer, order.colli, order.aantal);
                 return false; // Cannot determine status if parsing fails
            }

            bool isProcessed = colli > 0 && (aantal + order.gemeld) >= colli;
            // Log detailed status for debugging specific orders if needed
            // _logger.LogTrace("IsOrderProcessed check for OrderRegel {OrderRegel}: Colli={Colli}, Aantal={Aantal}, Gemeld={Gemeld}, Processed={IsProcessed}",
            //     order.orderregelnummer, colli, aantal, order.gemeld, isProcessed);
            return isProcessed;
        }

        // --- Helper Methode FilterProductsToScan ---
        private async Task FilterProductsToScan(List<Order> allVehicleOrders)
        {
            _logger.LogInformation("FilterProductsToScan called.");
            var heavyProductNamesLower = await _context.HeavyProducts
                .Select(hp => hp.Name.ToLower())
                .ToListAsync();
            _logger.LogInformation("Loaded {Count} heavy product names for filtering.", heavyProductNamesLower.Count);

            bool isRegularMode = SelectedMode == "regular";
            bool showAll = CurrentUser?.ShowAll ?? false;
            _logger.LogInformation("Filtering settings: IsRegularMode={IsRegular}, ShowAll={ShowAll}, SelectedCustomer={Customer}", isRegularMode, showAll, SelectedCustomer);

            IEnumerable<Order> filteredOrders = allVehicleOrders;

            // Stap 1: Filter op Product Type
            if (isRegularMode)
            {
                filteredOrders = allVehicleOrders
                    .Where(o => !heavyProductNamesLower.Any(hp =>
                        o.artikelomschrijving?.ToLower().Contains(hp) ?? false));
                _logger.LogInformation("Filtered for REGULAR products. Count before customer filter: {Count}", filteredOrders.Count());
            }
            else
            {
                filteredOrders = allVehicleOrders
                    .Where(o => heavyProductNamesLower.Any(hp =>
                        o.artikelomschrijving?.ToLower().Contains(hp) ?? false));
                _logger.LogInformation("Filtered for HEAVY products. Count before customer filter: {Count}", filteredOrders.Count());
            }

            // Order by sequence *after* type filtering, before customer selection/filtering
            filteredOrders = filteredOrders.OrderByDescending(o => o.volgorde);

            // Stap 2: Bepaal/Filter op Klant
            if (string.IsNullOrEmpty(SelectedCustomer))
            {
                var firstOrder = filteredOrders.FirstOrDefault();
                if (firstOrder != null)
                {
                    SelectedCustomer = firstOrder.klantnaam;
                    CurrentCustomerOrderInfo = firstOrder;
                    _logger.LogInformation("No customer selected, automatically selected first customer: {SelectedCustomer} based on volgorde.", SelectedCustomer);
                }
                 else
                {
                    _logger.LogWarning("No customer selected and no orders found after type filtering for mode {Mode}.", SelectedMode);
                }
            }

            // Filter by customer if not showing all
            if (!showAll && !string.IsNullOrEmpty(SelectedCustomer))
            {
                 _logger.LogInformation("Filtering for selected customer: {SelectedCustomer}", SelectedCustomer);
                filteredOrders = filteredOrders.Where(o => o.klantnaam == SelectedCustomer);
                if (CurrentCustomerOrderInfo == null) // Set if not set during auto-selection
                {
                    CurrentCustomerOrderInfo = filteredOrders.FirstOrDefault();
                }
                 _logger.LogInformation("Count after customer filter: {Count}", filteredOrders.Count());
            }
            else if (showAll)
            {
                 _logger.LogInformation("ShowAll is true, skipping customer filter.");
                 // Ensure CurrentCustomerOrderInfo is set for ShowAll mode if needed elsewhere,
                 // maybe based on the highest sequence number overall for the mode?
                 CurrentCustomerOrderInfo = filteredOrders.FirstOrDefault(); // Example: Set to the first overall in ShowAll
            }


            ProductsToScan = filteredOrders.ToList();
            _logger.LogInformation("FilterProductsToScan finished. ProductsToScan count: {Count}", ProductsToScan.Count);

            // Update status based on the *entire* vehicle's orders, but pass heavy names
            UpdateScanStatus(allVehicleOrders, heavyProductNamesLower);
        }


        // --- Status Update Logic ---
        private void UpdateScanStatus(List<Order> allVehicleOrders, List<string> heavyProductNamesLower)
        {
             _logger.LogInformation("UpdateScanStatus called.");
            if (ProductsToScan == null)
            {
                _logger.LogWarning("UpdateScanStatus: ProductsToScan is null, initializing.");
                ProductsToScan = new List<Order>();
                // Set flags to default/false state if ProductsToScan is null initially
                IsKlantFaseCompleet = false;
                IsVoertuigVolledigCompleet = false;
                ShowGoToRegularButton = false;
                NextCustomerId = null;
                return;
            }

            // Check of huidige klant/fase compleet is (based on the filtered ProductsToScan list)
            IsKlantFaseCompleet = !ProductsToScan.Any() || ProductsToScan.All(IsOrderProcessed);
            _logger.LogInformation("IsKlantFaseCompleet: {Status} (Based on {Count} ProductsToScan)", IsKlantFaseCompleet, ProductsToScan.Count);

            // Check of hele voertuig compleet is (based on all orders for the vehicle)
            IsVoertuigVolledigCompleet = !allVehicleOrders.Any() || allVehicleOrders.All(IsOrderProcessed);
             _logger.LogInformation("IsVoertuigVolledigCompleet: {Status} (Based on {Count} allVehicleOrders)", IsVoertuigVolledigCompleet, allVehicleOrders.Count);

            // Check of alle zware producten gescand zijn en er reguliere producten zijn
            var heavyOrders = allVehicleOrders
                .Where(o => heavyProductNamesLower.Any(hp =>
                    o.artikelomschrijving?.ToLower().Contains(hp) ?? false));

            var regularOrders = allVehicleOrders
                .Where(o => !heavyProductNamesLower.Any(hp =>
                    o.artikelomschrijving?.ToLower().Contains(hp) ?? false));

            bool allHeavyProcessed = !heavyOrders.Any() || heavyOrders.All(IsOrderProcessed);
            bool hasAnyRegular = regularOrders.Any();

            ShowGoToRegularButton = SelectedMode != "regular" && // Niet al in reguliere modus
                                   allHeavyProcessed &&          // Alle zware producten gescand
                                   hasAnyRegular;                // Er zijn reguliere producten
            _logger.LogInformation("ShowGoToRegularButton check: IsHeavyMode={IsHeavy}, AllHeavyProcessed={AllHeavy}, HasRegular={HasRegular} -> ShowButton={Show}",
                SelectedMode != "regular", allHeavyProcessed, hasAnyRegular, ShowGoToRegularButton);


            // Bepaal volgende klant alleen als huidige klant compleet is en we niet in ShowAll mode zijn
            if (IsKlantFaseCompleet && CurrentCustomerOrderInfo != null && !(CurrentUser?.ShowAll ?? false))
            {
                _logger.LogInformation("Current customer/phase complete and not in ShowAll mode. Finding next customer.");
                FindNextCustomer(allVehicleOrders, CurrentCustomerOrderInfo.volgorde);
            }
            else
            {
                 _logger.LogInformation("Skipping FindNextCustomer. IsKlantFaseCompleet={IsComplete}, CurrentCustomerOrderInfo exists={Exists}, ShowAll={ShowAll}",
                    IsKlantFaseCompleet, CurrentCustomerOrderInfo != null, CurrentUser?.ShowAll ?? false);
                 NextCustomerId = null; // Ensure NextCustomerId is null if conditions aren't met
            }
            
             _logger.LogInformation("UpdateScanStatus finished.");
        }

        // --- Helper Methode FindNextCustomer ---
        private void FindNextCustomer(List<Order> allVehicleOrders, int currentOrderSequence)
        {
             _logger.LogInformation("FindNextCustomer called. Current Sequence: {Sequence}", currentOrderSequence);
            bool isRegularMode = SelectedMode == "regular";
            var heavyProductNamesLower = _context.HeavyProducts // Consider passing this list if performance is critical
                .Select(hp => hp.Name.ToLower())
                .ToList();

            // Get distinct customers for current mode, ordered by sequence
            var customersInMode = allVehicleOrders
                .Where(o => isRegularMode
                    ? !heavyProductNamesLower.Any(hp => o.artikelomschrijving?.ToLower().Contains(hp) ?? false)
                    : heavyProductNamesLower.Any(hp => o.artikelomschrijving?.ToLower().Contains(hp) ?? false))
                .OrderByDescending(o => o.volgorde) // Order by sequence first
                .Select(o => o.klantnaam)
                .Distinct() // Then get unique names in that order
                .ToList();
             _logger.LogInformation("Customers in current mode ({Mode}), ordered by sequence: {CustomerList}", SelectedMode, string.Join(", ", customersInMode));

            var currentIndex = customersInMode.IndexOf(SelectedCustomer);
             _logger.LogInformation("Current customer '{SelectedCustomer}' found at index {Index} in the mode list.", SelectedCustomer, currentIndex);

            if (currentIndex != -1 && currentIndex < customersInMode.Count - 1)
            {
                NextCustomerId = customersInMode[currentIndex + 1];
                 _logger.LogInformation("Next customer determined: {NextCustomer}", NextCustomerId);
            }
            else
            {
                NextCustomerId = null;
                 _logger.LogInformation("No next customer found (current index {Index}, list count {Count}).", currentIndex, customersInMode.Count);
            }
        }
    }


    public class VehicleStatus
    {
        public string VehicleId { get; set; } = string.Empty;
        public int TotalProducts { get; set; }
        public int ScannedProducts { get; set; }
    }
}