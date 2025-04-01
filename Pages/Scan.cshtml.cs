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

namespace TestProject.Pages
{
    /// <summary>
    /// Page model for the product scanning interface.
    /// Requires user to be in Laadploeg, Planner, or Admin role.
    /// </summary>
    [Authorize(Roles = "Laadploeg, Planner, Admin")]
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
            // Initialize collections in constructor
            VehicleStatuses = new List<VehicleStatus>();
            RegularOrders = new List<Order>();
            HeavyOrders = new List<Order>();
            LosseArtikelenPerOrder = new Dictionary<string, List<LosseArtikelen>>();
        }

        /// <summary>
        /// List of heavy orders for the selected vehicle
        /// </summary>
        public IList<Order> HeavyOrders { get; set; } = new List<Order>();
        
        /// <summary>
        /// List of regular orders for the selected vehicle
        /// </summary>
        public IList<Order> RegularOrders { get; set; } = new List<Order>();
        
        /// <summary>
        /// List of all unique vehicle identifiers in the system
        /// </summary>
        public List<string> UniqueVehicles { get; set; } = new List<string>();
        
        /// <summary>
        /// Currently selected vehicle identifier
        /// </summary>
        public string SelectedVehicle { get; set; }
        
        /// <summary>
        /// Currently selected customer name
        /// </summary>
        public string CurrentCustomer { get; set; }

        /// <summary>
        /// Flag indicating if all heavy orders are scanned
        /// </summary>
        public bool AllHeavyOrdersScanned { get; set; }
        
        /// <summary>
        /// Flag indicating if this page should show regular orders instead of heavy orders
        /// </summary>
        public bool ShowRegularOrdersMode { get; set; }
        
        /// <summary>
        /// Customer with the highest volgorde value
        /// </summary>
        public string HighestVolgordeCustomer { get; set; }

        public List<VehicleStatus> VehicleStatuses { get; set; }

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
        public string NextCustomer { get; set; }

        /// <summary>
        /// Add new property to store loose articles
        /// </summary>
        public Dictionary<string, List<LosseArtikelen>> LosseArtikelenPerOrder { get; set; } = new Dictionary<string, List<LosseArtikelen>>();

        /// <summary>
        /// Handles navigation to regular orders page
        /// </summary>
        public IActionResult OnPostShowRegularOrders(string vehicle)
        {
            _logger.LogInformation($"Redirecting to regular orders for vehicle {vehicle}");
            return RedirectToPage("Scan", new { vehicle, mode = "regular" });
        }

        /// <summary>
        /// Handles navigation to the next customer's orders
        /// </summary>
        /// <param name="vehicle">The selected vehicle</param>
        /// <param name="currentCustomer">The current customer</param>
        /// <returns>Redirect action to the same page with updated customer</returns>
        public IActionResult OnPostNextCustomer(string vehicle, string currentCustomer)
        {
            if (string.IsNullOrEmpty(vehicle) || string.IsNullOrEmpty(currentCustomer))
            {
                return BadRequest(new { success = false, message = "Vehicle and current customer must be provided." });
            }

            // Get all customers for this vehicle
            var customers = _context.Orders
                .Where(o => o.voertuig == vehicle)
                .Select(o => o.klantnaam)
                .Distinct()
                .OrderByDescending(volgorde => volgorde)
                .ToList();

            if (customers == null || !customers.Any())
            {
                return NotFound(new { success = false, message = "No customers found for this vehicle." });
            }

            // Find current customer index and get next customer
            var currentIndex = customers.IndexOf(currentCustomer);
            if (currentIndex == -1)
            {
                return NotFound(new { success = false, message = "Current customer not found in the list." });
            }

            if (currentIndex < customers.Count - 1)
            {
                var nextCustomer = customers[currentIndex + 1];
                return RedirectToPage("Scan", new { vehicle, mode = "regular", customer = nextCustomer });
            }

            // If no next customer, just stay on current page
            return RedirectToPage("Scan", new { vehicle, mode = "regular", customer = currentCustomer });
        }

        /// <summary>
        /// Handles GET requests to the page
        /// Loads vehicle data and orders based on the selected vehicle and customer
        /// </summary>
        /// <param name="vehicle">Optional vehicle identifier from query string</param>
        /// <param name="mode">Optional mode parameter to show regular orders</param>
        /// <param name="customer">Optional customer name from query string</param>
        public async Task<IActionResult> OnGetAsync(string vehicle = null, string mode = null, string customer = null)
        {
            try
            {
                SelectedVehicle = vehicle;
                ShowRegularProductsMode = mode == "regular";
                CurrentCustomer = customer;

                // Always get vehicle statuses
                VehicleStatuses = await GetVehicleStatusesAsync();

                if (!string.IsNullOrEmpty(vehicle))
                {
                    _logger.LogInformation($"Loading orders for vehicle: {vehicle}");

                    var allOrders = await _context.Orders
                        .Where(o => o.voertuig == vehicle)
                        .ToListAsync();

                    // Get all loose articles that correspond to the orders
                    var orderRegelnummers = allOrders.Select(o => o.orderregelnummer).ToList();
                    var losseArtikelen = await _context.LosseArtikelen
                        .Where(la => orderRegelnummers.Contains(la.orderid))
                        .ToListAsync();

                    // Group loose articles by order number
                    LosseArtikelenPerOrder = losseArtikelen
                        .GroupBy(la => la.orderid)
                        .ToDictionary(g => g.Key, g => g.ToList());

                    _logger.LogInformation($"Found {losseArtikelen.Count} loose articles for {LosseArtikelenPerOrder.Count} orders");

                    var heavyProductNames = await _context.HeavyProducts
                        .Select(hp => hp.Name)
                        .ToListAsync();

                    _logger.LogInformation($"Found {heavyProductNames.Count} heavy product names: {string.Join(", ", heavyProductNames)}");

                    // Get all unique customers ordered by name
                    CustomerList = allOrders
                        .Select(o => o.klantnaam)
                        .Distinct()
                        .OrderBy(k => k)
                        .ToList();

                    _logger.LogInformation($"Found {CustomerList.Count} unique customers");

                    // Split orders into heavy and regular
                    HeavyOrders = allOrders
                        .Where(o => heavyProductNames.Any(hp => 
                            o.artikelomschrijving != null && 
                            o.artikelomschrijving.ToLower().Contains(hp.ToLower())))
                        .OrderBy(o => o.klantnaam)
                        .ThenBy(o => o.orderregelnummer)
                        .ToList();

                    _logger.LogInformation($"Found {HeavyOrders.Count} heavy orders");

                    // If there are no heavy orders or we're in regular mode
                    if (!HeavyOrders.Any() || ShowRegularProductsMode)
                    {
                        // If no customer is selected, take the first one
                        if (string.IsNullOrEmpty(CurrentCustomer) && CustomerList.Any())
                        {
                            CurrentCustomer = CustomerList.First();
                            _logger.LogInformation($"Selected first customer: {CurrentCustomer}");
                        }

                        // Find the next customer in the list
                        var currentIndex = CustomerList.IndexOf(CurrentCustomer);
                        NextCustomer = currentIndex < CustomerList.Count - 1 ? CustomerList[currentIndex + 1] : null;

                        // Get regular orders for current customer
                        RegularOrders = allOrders
                            .Where(o => !heavyProductNames.Any(hp => 
                                o.artikelomschrijving != null && 
                                o.artikelomschrijving.ToLower().Contains(hp.ToLower())))
                            .Where(o => o.klantnaam == CurrentCustomer)
                            .OrderByDescending(o => o.volgorde)
                            .ToList();

                        _logger.LogInformation($"Found {RegularOrders.Count} regular orders for customer {CurrentCustomer}");
                        ShowRegularProductsMode = true;
                    }

                    // Check if all heavy orders are scanned
                    AllHeavyOrdersScanned = HeavyOrders.Any() && HeavyOrders.All(o => o.gescand);
                    _logger.LogInformation($"ShowRegularProductsMode: {ShowRegularProductsMode}, AllHeavyOrdersScanned: {AllHeavyOrdersScanned}");
                }

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OnGetAsync");
                VehicleStatuses = new List<VehicleStatus>();
                RegularOrders = new List<Order>();
                HeavyOrders = new List<Order>();
                LosseArtikelenPerOrder = new Dictionary<string, List<LosseArtikelen>>();
                return Page();
            }
        }

        private async Task<List<VehicleStatus>> GetVehicleStatusesAsync()
        {
            var vehicles = await _context.Orders
                .Select(p => p.voertuig)
                .Distinct()
                .Where(v => v != null)
                .ToListAsync();

            var statuses = new List<VehicleStatus>();

            foreach (var vehicleId in vehicles)
            {
                var orders = await _context.Orders
                    .Where(p => p.voertuig == vehicleId)
                    .ToListAsync();

                var scannedCount = orders.Count(p => p.gescand);
                var totalCount = orders.Count;

                var status = "not-started";
                if (scannedCount == totalCount)
                    status = "completed";
                else if (scannedCount > 0)
                    status = "in-progress";

                statuses.Add(new VehicleStatus
                {
                    VehicleId = vehicleId,
                    Status = status,
                    ScannedProducts = scannedCount,
                    TotalProducts = totalCount
                });
            }

            return statuses;
        }
    }

    public class VehicleStatus
    {
        public string VehicleId { get; set; }
        public string Status { get; set; }
        public int ScannedProducts { get; set; }
        public int TotalProducts { get; set; }
    }
}