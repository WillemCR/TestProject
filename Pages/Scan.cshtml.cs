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
        /// Handles navigation to the next customer
        /// </summary>
        public IActionResult OnPostNextCustomer(string vehicle, string nextCustomer)
        {
            return RedirectToPage("Scan", new { vehicle = vehicle, customer = nextCustomer });
        }

        /// <summary>
        /// Handles GET requests to the page
        /// Loads vehicle data and products based on the selected vehicle and customer
        /// </summary>
        /// <param name="vehicle">Optional vehicle identifier from query string</param>
        /// <param name="customer">Optional customer name from query string</param>
        public async Task OnGetAsync(string vehicle = null, string customer = null)
        {
            // Get unique vehicles from the database for the dropdown
            UniqueVehicles = await _context.Products
                .Select(p => p.voertuig)
                .Distinct()
                .Where(v => v != null)
                .OrderBy(v => v)
                .ToListAsync();

            SelectedVehicle = vehicle;

            if (!string.IsNullOrEmpty(vehicle))
            {
                // Get all heavy product names from the database
                var heavyProductNames = await _context.HeavyProducts
                    .Select(hp => hp.Name)
                    .ToListAsync();

                // Get all products for the selected vehicle
                var allProducts = await _context.Products
                    .Where(p => p.voertuig == vehicle)
                    .OrderByDescending(p => p.volgorde)
                    .ToListAsync();
                    
                // Get all customer names for this vehicle, ordered by some criteria
                var customerNames = allProducts
                    .GroupBy(p => p.klantnaam)
                    .Select(g => new
                    {
                        CustomerName = g.Key,
                        HasHeavyProducts = g.Any(p => heavyProductNames.Any(hp => 
                            p.artikelomschrijving != null && 
                            p.artikelomschrijving.Contains(hp))),
                        FirstVolgorde = g.Max(p => p.volgorde)
                    })
                    .OrderByDescending(c => c.HasHeavyProducts)  // Sort heavy products first
                    .ThenBy(c => c.FirstVolgorde)               // Then by volgorde
                    .Select(c => c.CustomerName)
                    .ToList();
                    
                // If no customer is specified or the specified customer is not valid,
                // use the first customer in the list
                if (string.IsNullOrEmpty(customer) || !customerNames.Contains(customer))
                {
                    CurrentCustomer = customerNames.FirstOrDefault();
                }
                else
                {
                    CurrentCustomer = customer;
                }
                
                // Filter products by current customer
                var filteredProducts = allProducts.Where(p => p.klantnaam == CurrentCustomer).ToList();
                
                // Split filtered products into heavy and regular categories
                HeavyProducts = filteredProducts
                    .Where(p => heavyProductNames.Any(hp => p.artikelomschrijving != null && p.artikelomschrijving.Contains(hp)))
                    .ToList();

                RegularProducts = filteredProducts
                    .Where(p => !heavyProductNames.Any(hp => p.artikelomschrijving != null && p.artikelomschrijving.Contains(hp)))
                    .ToList();
                
                // Check if all products for current customer have been scanned
                bool allCurrentCustomerProductsScanned = filteredProducts.All(p => p.gescand || p.gemeld > 0);
                
                // If all products for current customer are scanned, get the next customer
                if (allCurrentCustomerProductsScanned && customerNames.Count > 1)
                {
                    // Find the index of the current customer
                    int currentIndex = customerNames.IndexOf(CurrentCustomer);
                    
                    // If there's a next customer, store it for the view to use
                    if (currentIndex < customerNames.Count - 1)
                    {
                        ViewData["NextCustomer"] = customerNames[currentIndex + 1];
                    }
                }
                
                // Check if all products for this vehicle have been scanned
                CheckAllProductsScanned();
            }
        }
        
        /// <summary>
        /// Checks if all products for the selected vehicle have been scanned or reported as missing
        /// </summary>
        private void CheckAllProductsScanned()
        {
            if (string.IsNullOrEmpty(SelectedVehicle))
                return;
                
            // A product is considered processed if it's either scanned or reported as missing
            bool allHeavyScanned = HeavyProducts.All(p => p.gescand || p.gemeld > 0);
            bool allRegularScanned = RegularProducts.All(p => p.gescand || p.gemeld > 0);
            
            // If all products are processed and there are products to process
            if (allHeavyScanned && allRegularScanned && (HeavyProducts.Count > 0 || RegularProducts.Count > 0))
            {
                // Notify that all products for this vehicle are processed
                NotifyVehicleComplete();
            }
        }
        
        /// <summary>
        /// Placeholder method for future Navision integration
        /// Will be implemented to send a message to Navision when all products are processed
        /// </summary>
        protected virtual void NotifyVehicleComplete()
        {
            // This method will be implemented in the future to send a message to Navision
            // For now, it's just a placeholder that logs to the console
            Console.WriteLine($"Alle producten voor voertuig {SelectedVehicle} zijn gescand of gemeld.");
        }
    }
}