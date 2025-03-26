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

        /// <summary>
        /// Constructor that injects the database context
        /// </summary>
        /// <param name="context">The application database context</param>
        public ScanModel(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// List of heavy products for the selected vehicle
        /// </summary>
        public IList<Product> HeavyProducts { get; set; } = new List<Product>();
        
        /// <summary>
        /// List of regular products for the selected vehicle
        /// </summary>
        public IList<Product> RegularProducts { get; set; } = new List<Product>();
        
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
        /// Flag indicating if all heavy products are scanned
        /// </summary>
        public bool AllHeavyProductsScanned { get; set; }
        
        /// <summary>
        /// Flag indicating if this page should show regular products instead of heavy products
        /// </summary>
        public bool ShowRegularProductsMode { get; set; }
        
        /// <summary>
        /// Customer with the highest volgorde value
        /// </summary>
        public string HighestVolgordeCustomer { get; set; }

        /// <summary>
        /// Handles navigation to regular products page
        /// </summary>
        public IActionResult OnPostShowRegularProducts(string vehicle)
        {
            return RedirectToPage("Scan", new { vehicle = vehicle, mode = "regular" });
        }

        /// <summary>
        /// Handles navigation to the next customer's products
        /// </summary>
        /// <param name="vehicle">The selected vehicle</param>
        /// <param name="nextCustomer">The name of the next customer to display</param>
        /// <returns>Redirect action to the same page with updated customer</returns>
        public IActionResult OnPostNextCustomer(string vehicle, string nextCustomer)
        {
            return RedirectToPage("Scan", new { vehicle = vehicle, mode = "regular", customer = nextCustomer });
        }

        /// <summary>
        /// Handles GET requests to the page
        /// Loads vehicle data and products based on the selected vehicle and customer
        /// </summary>
        /// <param name="vehicle">Optional vehicle identifier from query string</param>
        /// <param name="customer">Optional customer name from query string</param>
        /// <param name="mode">Optional mode parameter to show regular products</param>
        public async Task<IActionResult> OnGetAsync(string vehicle = null, string customer = null, string mode = null)
        {
            // Get unique vehicles from the database for the dropdown
            UniqueVehicles = await _context.Products
                .Select(p => p.voertuig)
                .Distinct()
                .Where(v => v != null)
                .OrderBy(v => v)
                .ToListAsync();

            SelectedVehicle = vehicle;
            ShowRegularProductsMode = mode == "regular";

            if (!string.IsNullOrEmpty(vehicle))
            {
                // Get all heavy product names from the database
                var heavyProductNames = await _context.HeavyProducts
                    .Select(hp => hp.Name)
                    .ToListAsync();

                // Get all products for the selected vehicle
                var allProducts = await _context.Products
                    .Where(p => p.voertuig == vehicle)
                    .OrderBy(p => p.volgorde) // Sort by volgorde
                    .ToListAsync();

                // Get all heavy products regardless of customer
                HeavyProducts = allProducts
                    .Where(p => heavyProductNames.Any(hp => 
                        p.artikelomschrijving != null && 
                        p.artikelomschrijving.Contains(hp)))
                        .OrderByDescending(p => p.volgorde)
                    .ToList();

                // Check if all heavy products are scanned
                AllHeavyProductsScanned = HeavyProducts.All(p => p.gescand);

                // Get normal products grouped by customer
                var customerGroups = allProducts
                    .Where(p => !heavyProductNames.Any(hp => 
                        p.artikelomschrijving != null && 
                        p.artikelomschrijving.Contains(hp)))
                    .GroupBy(p => p.klantnaam)
                    .Select(g => new
                    {
                        CustomerName = g.Key,
                        Products = g.OrderByDescending(p => p.volgorde).ToList(),
                        MaxVolgorde = g.Max(p => p.volgorde)
                    })
                    .OrderByDescending(g => g.MaxVolgorde)
                    .ToList();

                // Find customer with highest volgorde
                HighestVolgordeCustomer = customerGroups.FirstOrDefault()?.CustomerName;

                // Auto-redirect to regular products if there are no heavy products
                if (!HeavyProducts.Any() && !ShowRegularProductsMode)
                {
                    return RedirectToPage("Scan", new { vehicle = vehicle, mode = "regular" });
                }

                if (ShowRegularProductsMode && AllHeavyProductsScanned)
                {
                    // Set the current customer if specified, otherwise use the one with highest volgorde
                    if (!string.IsNullOrEmpty(customer) && customerGroups.Any(c => c.CustomerName == customer))
                    {
                        CurrentCustomer = customer;
                    }
                    else
                    {
                        CurrentCustomer = HighestVolgordeCustomer;
                    }

                    // Show regular products for the current customer
                    RegularProducts = customerGroups
                        .FirstOrDefault(c => c.CustomerName == CurrentCustomer)?.Products ?? new List<Product>();
                    
                    // Set the next customer if available AND all current customer products are scanned
                    int currentIndex = customerGroups.FindIndex(c => c.CustomerName == CurrentCustomer);
                    bool allCurrentCustomerProductsScanned = RegularProducts.All(p => p.gescand);
                    
                    if (currentIndex < customerGroups.Count - 1 && allCurrentCustomerProductsScanned)
                    {
                        string nextCustomer = customerGroups[currentIndex + 1].CustomerName;
                        ViewData["NextCustomer"] = nextCustomer;
                    }
                }
                else
                {
                    // In heavy products mode, only show heavy products
                    RegularProducts = new List<Product>();
                    CurrentCustomer = null;
                }
            }

            return Page();
        }
        
        /// <summary>
        /// Checks if all products for the selected vehicle have been scanned or reported as missing
        /// </summary>
    //     private void CheckAllProductsScanned()
    //     {
    //         if (string.IsNullOrEmpty(SelectedVehicle))
    //             return;
                
    //         // A product is considered processed if it's scanned
    //         bool allHeavyScanned = HeavyProducts.All(p => p.gescand);
    //         bool allRegularScanned = RegularProducts.All(p => p.gescand);
            
    //         // If all products are processed and there are products to process
    //         if (allHeavyScanned && allRegularScanned && (HeavyProducts.Count > 0 || RegularProducts.Count > 0))
    //         {
    //             // Notify that all products for this vehicle are processed
    //             NotifyVehicleComplete();
    //         }
    //     }
        
    //     /// <summary>
    //     /// Placeholder method for future Navision integration
    //     /// Will be implemented to send a message to Navision when all products are processed
    //     /// </summary>
    //     protected virtual void NotifyVehicleComplete()
    //     {
    //         // This method will be implemented in the future to send a message to Navision
    //         // For now, it's just a placeholder that logs to the console
    //         Console.WriteLine($"Alle producten voor voertuig {SelectedVehicle} zijn gescand of gemeld.");
    //     }
    // }
}
}