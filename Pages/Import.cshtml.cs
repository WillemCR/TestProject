using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TestProject.Data;
using TestProject.Models;

namespace TestProject.Pages
{
    public class ImportModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ImportModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public IFormFile File { get; set; }

        public ImportResult ImportStatus { get; set; }

        public class ImportResult
        {
            public bool Success { get; set; }
            public string Message { get; set; }
        }

  public async Task<IActionResult> OnPostAsync()
{
    if (File == null || File.Length == 0)
    {
        ImportStatus = new ImportResult { Success = false, Message = "Please select a file to upload." };
        return Page();
    }

    try
    {
        using (var stream = new MemoryStream())
        {
            await File.CopyToAsync(stream);
            using (var package = new ExcelPackage(stream))
            {
                var worksheet = package.Workbook.Worksheets[0];
                var rowCount = worksheet.Dimension.Rows;

                for (int row = 2; row <= rowCount; row++)
                {
                   
                    // Parse 'datum' with validation
                    if (!DateTime.TryParse(worksheet.Cells[row, 8].Text, out DateTime datumValue))
                    {
                        continue; // Skip invalid date
                    }

                    // Handle numeric fields with TryParse and default to 0
                    int opgehaaldValue = 0;
                    int.TryParse(worksheet.Cells[row, 10].Text.Replace("NULL", ""), out opgehaaldValue);

                    int aantalValue = 0;
                    int.TryParse(worksheet.Cells[row, 17].Text, out aantalValue);

                    int volgordeValue = 0;
                    int.TryParse(worksheet.Cells[row, 21].Text, out volgordeValue);

                    int colliValue = 0;
                    int.TryParse(worksheet.Cells[row,16].Text, out colliValue);

                    var product = new Order
                    {
                        orderno = worksheet.Cells[row, 2].Text,
                        verzendwijze = worksheet.Cells[row, 3].Text,
                        klantnaam = worksheet.Cells[row, 4].Text,
                        klantnummer = worksheet.Cells[row, 5].Text,
                        status = worksheet.Cells[row, 6].Text,
                        orderregelnummer = worksheet.Cells[row, 7].Text,
                        datum = datumValue,
                        gefactureerd = worksheet.Cells[row, 9].Text,
                        opgehaald = opgehaaldValue,
                        losartikel = worksheet.Cells[row, 11].Text == "1" ? 1 : 0,
                        handtekening = worksheet.Cells[row, 12].Text,
                        bron = worksheet.Cells[row, 13].Text,
                        artikelomschrijving = worksheet.Cells[row, 14].Text,
                        lengte = worksheet.Cells[row, 15].Text,
                        colli = colliValue.ToString(),
                        aantal = aantalValue.ToString(),
                        referentie = worksheet.Cells[row, 18].Text,
                        voertuig = worksheet.Cells[row, 19].Text,
                        adres = worksheet.Cells[row, 20].Text,
                        volgorde = volgordeValue
                    };

                    // Check for existing product
                    var existingProduct = await _context.Orders.FirstOrDefaultAsync(p => p.orderregelnummer == product.orderregelnummer);
                    if (existingProduct == null)
                    {
                        await _context.Orders.AddAsync(product);
                    }
                }

                await _context.SaveChangesAsync();
            }
        }

        ImportStatus = new ImportResult { Success = true, Message = "Products imported successfully!" };
    }
  catch (Exception ex)
    {
        // Here we'll handle and display the inner exception
        string errorMessage = ex.Message;
        Exception innerException = ex.InnerException;
        while (innerException != null)
        {
            errorMessage += $"\nInner Exception: {innerException.Message}";
            innerException = innerException.InnerException;
        }

        ImportStatus = new ImportResult { 
            Success = false, 
            Message = $"Error importing products: {errorMessage}"
        };
    }


    return Page();
}

        public async Task<IActionResult> OnPostFlushProductsAsync()
        {
            try
            {
                var products = await _context.Orders.ToListAsync();
                _context.Orders.RemoveRange(products);
                await _context.SaveChangesAsync();

                ImportStatus = new ImportResult
                {
                    Success = true,
                    Message = "Products table flushed successfully!"
                };
            }
            catch (Exception ex)
            {
                ImportStatus = new ImportResult
                {
                    Success = false,
                    Message = $"Error flushing Products table: {ex.Message}"
                };
            }

            return Page();
        }
    }
}