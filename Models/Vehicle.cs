using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TestProject.Models;

public class Vehicle
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string Name { get; set; }

    public decimal? Length { get; set; }
    public decimal? Height { get; set; }
    public decimal? Width { get; set; }

    // Foreign key for Crew
    public int? CrewId { get; set; }

    // Navigation property for Crew
    public Crew Crew { get; set; }
}