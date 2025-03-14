using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TestProject.Models
{
    public class Product
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
      public int id { get; set; }

        [Required]
        public string orderno { get; set; } = string.Empty;

        public string verzendwijze { get; set; } = string.Empty;

        public string klantnaam { get; set; } = string.Empty;

        public string klantnummer { get; set; } = string.Empty;

        public string status { get; set; } = string.Empty;

        public string orderregelnummer { get; set; } = string.Empty;

        public DateTime datum { get; set; } = default(DateTime);

        public string gefactureerd { get; set; } = string.Empty;

        public int opgehaald { get; set; } = 0;
        
        public int losartikel { get; set; } = 0;

        public string handtekening { get; set; } = string.Empty;

        public string bron { get; set; } = string.Empty;

        public string artikelomschrijving { get; set; } = string.Empty;

        public string lengte { get; set; } = string.Empty;

        public int colli { get; set; } = 0;
        
        public int aantal { get; set; } = 0;

        public string referentie { get; set; } = string.Empty;

        public string voertuig { get; set; } = string.Empty;

        public string adres { get; set; } = string.Empty;
      
        public int volgorde { get; set; } = 0;

        public bool gescand { get; set; } = false;

         public int gemeld { get; set; } = 0;

    }
}
