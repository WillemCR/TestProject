using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TestProject.Models;

public class Vehicle
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string Name { get; set; }

    [Required]
    [Range(0.1, 200)]
    public decimal? Length { get; set; }

    [Required]
    [Range(0.1, 200)]
    public decimal? Height { get; set; }

    [Required]
    [Range(0.1, 200)]
    public decimal? Width { get; set; }

  
}
