using System;
using System.Collections.Generic;
using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using TestProject.Models;
using BCrypt.Net;
using Microsoft.AspNetCore.Identity;
namespace TestProject.Data
{
    public static class SeedData
    {
        public static void Initialize(ApplicationDbContext context)
        {
            // Ensure the database is created
            context.Database.EnsureCreated();

            // Seed HeavyProducts if it's empty
            if (!context.HeavyProducts.Any())
            {
                var heavyProductNames = new[]
                {
                    "HZ-V6000",
                    "HZ-T2000",
                    "HZ-T2200",
                    "HZ-T2600",
                    "HZ-D3400",
                    "BC-KNIX",
                    "BC-PRONTO"
                };

                context.HeavyProducts.AddRange(
                    heavyProductNames.Select(name => new HeavyProduct { Name = name })
                );

                context.SaveChanges();
            }
        }
        public static async Task SeedUsersAsync(this ApplicationDbContext dbContext, UserManager<User> userManager)
{
    if (dbContext.Users.Any())
    {
        return; // Seed data only if the database is empty.
    }

    UserRole[] roles = { UserRole.Laadploeg, UserRole.Planner };

   var users = new List<(User user, string password)>
    {
        (new User
        {
            Name = "Willem",
            LastLoggedIn = DateTime.Now,
            Role = UserRole.Admin,
            UserName = "willem@example.com",
            Email = "willem@example.com"
        }, "pass123"),
        (new User
        {
            Name = "John Doe",
            LastLoggedIn = DateTime.Now.AddDays(-1),
            Role = roles[new Random().Next(roles.Length)],
            UserName = "john.doe@example.com",
            Email = "john.doe@example.com"
        }, "pass123"),
        (new User
        {
            Name = "Jane Smith",
            LastLoggedIn = DateTime.Now.AddDays(-2),
            Role = roles[new Random().Next(roles.Length)],
            UserName = "jane.smith@example.com",
            Email = "jane.smith@example.com"
        }, "pass123"),
        (new User
        {
            Name = "Bob Johnson",
            LastLoggedIn = DateTime.Now.AddDays(-3),
            Role = roles[new Random().Next(roles.Length)],
            UserName = "bob.johnson@example.com",
            Email = "bob.johnson@example.com"
        }, "pass123"),
        (new User
        {
            Name = "Alice Brown",
            LastLoggedIn = DateTime.Now.AddDays(-4),
            Role = roles[new Random().Next(roles.Length)],
            UserName = "alice.brown@example.com",
            Email = "alice.brown@example.com"
        }, "pass123"),
        (new User
        {
            Name = "Charlie Wilson",
            LastLoggedIn = DateTime.Now.AddDays(-5),
            Role = roles[new Random().Next(roles.Length)],
            UserName = "charlie.wilson@example.com",
            Email = "charlie.wilson@example.com"
        }, "pass123"),
        (new User
        {
            Name = "Diana Miller",
            LastLoggedIn = DateTime.Now.AddDays(-6),
            Role = roles[new Random().Next(roles.Length)],
            UserName = "diana.miller@example.com",
            Email = "diana.miller@example.com"
        }, "pass123"),
        (new User
        {
            Name = "Edward Davis",
            LastLoggedIn = DateTime.Now.AddDays(-7),
            Role = roles[new Random().Next(roles.Length)],
            UserName = "edward.davis@example.com",
            Email = "edward.davis@example.com"
        }, "pass123"),
        (new User
        {
            Name = "Fiona Clark",
            LastLoggedIn = DateTime.Now.AddDays(-8),
            Role = roles[new Random().Next(roles.Length)],
            UserName = "fiona.clark@example.com",
            Email = "fiona.clark@example.com"
        }, "pass123"),
        (new User
        {
            Name = "George White",
            LastLoggedIn = DateTime.Now.AddDays(-9),
            Role = roles[new Random().Next(roles.Length)],
            UserName = "george.white@example.com",
            Email = "george.white@example.com"
        }, "pass123")
    };
    foreach (var (user, password) in users)
    {
        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            throw new Exception($"Failed to create user {user.UserName}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }
    }
}
}
}