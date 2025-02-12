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

    public DbSet<Product> Products { get; set; }
    public DbSet<Crew> Crews { get; set; }
    public DbSet<Vehicle> Vehicles { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Crew>()
            .HasOne(c => c.Vehicle)
            .WithOne(v => v.Crew)
            .HasForeignKey<Vehicle>(v => v.CrewId);

        builder.Entity<Crew>()
            .HasMany(c => c.Users)
            .WithOne(u => u.Crew)
            .HasForeignKey(u => u.CrewId);
    }
}
}
