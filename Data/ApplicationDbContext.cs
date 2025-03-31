using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TestProject.Models;
using Microsoft.AspNetCore.Identity;

namespace TestProject.Data
{
    public class ApplicationDbContext : IdentityDbContext<User, IdentityRole<int>, int>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Order> Orders { get; set; }
    public DbSet<HeavyProduct> HeavyProducts { get; set; }
    public DbSet<MissingProductReportEntity> MissingProductReports { get; set; }
    public DbSet<LosseArtikelen> LosseArtikelen { get; set; }
}
}
