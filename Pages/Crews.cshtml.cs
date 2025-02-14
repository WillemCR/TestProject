using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using TestProject.Models;
using TestProject.Data;
using Microsoft.AspNetCore.Authorization;

namespace TestProject.Pages
{
    [Authorize(Roles = "Admin")]
    public class CrewsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CrewsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<Crew> Crews { get; set; }

        public async Task OnGetAsync()
        {
            Crews = await _context.Crews
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var crew = await _context.Crews.FindAsync(id);
            if (crew != null)
            {
                _context.Crews.Remove(crew);
                await _context.SaveChangesAsync();
            }
            return RedirectToPage();
        }
    }
} 