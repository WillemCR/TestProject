using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace TestProject.Models
{
    public class User : IdentityUser<int>
    {
       
        public DateTime? LastLoggedIn { get; set; }
         
        [Required]
        public UserRole Role { get; set; }
        
    }

    public enum UserRole
    {
        Laadploeg,
        Planner,
        Admin
    }
}
