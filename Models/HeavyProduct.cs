using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TestProject.Models
{
    public class HeavyProduct
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}