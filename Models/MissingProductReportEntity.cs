using System;

namespace TestProject.Models
{
    public class MissingProductReportEntity
    {
        public int Id { get; set; }
        public string OrderregelNummer { get; set; }
        public string Artikelomschrijving { get; set; }
        public string Reason { get; set; }
        public int Amount {get; set;}
        public string Comments { get; set; }
        public DateTime ReportedAt { get; set; }
        public string ReportedBy { get; set; }
    }
}