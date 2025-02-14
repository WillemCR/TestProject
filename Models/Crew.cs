using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace TestProject.Models
{
public class Crew
    {
        [Key]
        public int CrewId { get; set; }

        [Required]
        public string Name { get; set; }

        public virtual ICollection<User>? Users { get; set; }
        public virtual Vehicle? Vehicle { get; set; }
    }
}