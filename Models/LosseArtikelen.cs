using System.ComponentModel.DataAnnotations;

namespace TestProject.Models
{
    public class LosseArtikelen
    {   
        public int Id { get; set; }
        [Required]
        public string orderid { get; set; }
        public string omschrijving { get; set; }
        public string artikelno { get; set; }
        public string lengte { get; set; }
        public int aantal { get; set; }
    }
} 