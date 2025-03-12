using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using TestProject.Models;
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
            LastLoggedIn = DateTime.Now,
            Role = UserRole.Admin,
            UserName = "Willem",
            Email = "willem@example.com"
        }, "pass123"),
        (new User
        {
            LastLoggedIn = DateTime.Now.AddDays(-1),
            Role = roles[new Random().Next(roles.Length)],
            UserName = "JohnDoe",
            Email = "john.doe@example.com"
        }, "pass123"),
        (new User
        {
            LastLoggedIn = DateTime.Now.AddDays(-2),
            Role = roles[new Random().Next(roles.Length)],
            UserName = "JaneSmith",
            Email = "jane.smith@example.com"
        }, "pass123"),
        (new User
        {
            LastLoggedIn = DateTime.Now.AddDays(-3),
            Role = roles[new Random().Next(roles.Length)],
            UserName = "BobJohnson",
            Email = "bob.johnson@example.com"
        }, "pass123"),
        (new User
        {
            LastLoggedIn = DateTime.Now.AddDays(-4),
            Role = roles[new Random().Next(roles.Length)],
            UserName = "AliceBrown",
            Email = "alice.brown@example.com"
        }, "pass123"),
        (new User
        {
            LastLoggedIn = DateTime.Now.AddDays(-5),
            Role = roles[new Random().Next(roles.Length)],
            UserName = "CharlieWilson",
            Email = "charlie.wilson@example.com"
        }, "pass123"),
        (new User
        {
            LastLoggedIn = DateTime.Now.AddDays(-6),
            Role = roles[new Random().Next(roles.Length)],
            UserName = "DianaMiller",
            Email = "diana.miller@example.com"
        }, "pass123"),
        (new User
        {
            LastLoggedIn = DateTime.Now.AddDays(-7),
            Role = roles[new Random().Next(roles.Length)],
            UserName = "EdwardDavis",
            Email = "edward.davis@example.com"
        }, "pass123"),
        (new User
        {
            LastLoggedIn = DateTime.Now.AddDays(-8),
            Role = roles[new Random().Next(roles.Length)],
            UserName = "FionaClark",
            Email = "fiona.clark@example.com"
        }, "pass123"),
        (new User
        {
            LastLoggedIn = DateTime.Now.AddDays(-9),
            Role = roles[new Random().Next(roles.Length)],
            UserName = "GeorgeWhite",
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