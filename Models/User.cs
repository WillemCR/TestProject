using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace TestProject.Models
{
    public class User : IdentityUser<int>
    {
        public string Name { get; set; }
        public bool IsAdmin { get; set; } = false;
        public DateTime? LastLoggedIn { get; set; }
    }
}

