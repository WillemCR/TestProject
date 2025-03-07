using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch.Internal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using TestProject.Data;

namespace TestProject.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Laadploeg, Planner, Admin")]
    public class ScanApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ScanApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost("ProcessBarcode")]
public async Task<IActionResult> ProcessBarcode([FromBody] ScanRequest request)
{
    try
    {
        Console.WriteLine("ProcessBarcode API endpoint reached");
        
        if (request == null || string.IsNullOrEmpty(request.Barcode))
        {
            return BadRequest(new { success = false, message = "Barcode is verplicht" });
        }

        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.orderregelnummer == request.Barcode);
        
        if (product == null)
        {
            return NotFound(new { success = false, message = "Product niet gevonden" });
        }

        product.aantal += 1;
        bool isComplete = product.aantal == int.Parse(product.colli);
        if (isComplete)
        {
            product.gescand = true;
        }
    
        await _context.SaveChangesAsync();
        
        // Return additional information
        return Ok(new { 
            success = true, 
            message = "Product succesvol gescand",
            aantal = product.aantal,
            colli = int.Parse(product.colli),
            isComplete = isComplete
        });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"API Exception: {ex.Message}");
        return StatusCode(500, new { success = false, message = ex.Message });
    }
}

        public class ScanRequest
        {
            public string Barcode { get; set; }
        }
    }
}