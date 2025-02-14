using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;
using TestProject.Models;
using TestProject.Data;
using Microsoft.AspNetCore.Authorization;

namespace TestProject.Pages.Shared
{
    [Authorize(Roles = "Admin")]
    public class CrewFormModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CrewFormModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Crew Crew { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id.HasValue && id.Value != 0)
            {
                Crew = await _context.Crews.FindAsync(id.Value);
                if (Crew == null)
                {
                    return NotFound();
                }
            }
            else
            {
                Crew = new Crew();
            }

            return Page();
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrWhiteSpace(Crew.Name))
            {
                ModelState.AddModelError("Crew.Name", "Name is required");
            }

            if (!ModelState.IsValid)
            {
                // Debug code to see what validation errors exist
                foreach (var modelStateEntry in ModelState.Values)
                {
                    foreach (var error in modelStateEntry.Errors)
                    {
                        Console.WriteLine($"Validation Error: {error.ErrorMessage}");
                    }
                }
                return Page();
            }

            try 
            {
                if (Crew.CrewId == 0)
                {
                    _context.Crews.Add(Crew);
                }
                else
                {
                    var existingCrew = await _context.Crews.FindAsync(Crew.CrewId);
                    if (existingCrew == null)
                    {
                        ModelState.AddModelError("", "Crew not found");
                        return Page();
                    }

                    _context.Entry(existingCrew).CurrentValues.SetValues(Crew);
                }

                await _context.SaveChangesAsync();
                return RedirectToPage("/Crews");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An error occurred while saving the crew.");
                return Page();
            }
        }
    }
} 