using System;
using System.Collections.Generic;
using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using TestProject.Models;
using BCrypt.Net;
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
        public static void SeedUsers(this ApplicationDbContext dbContext)
        {
            if (dbContext.Users.Any())
            {
                return; // Seed data only if the database is empty.
            }

            
            UserRole[] roles = { UserRole.Laadploeg, UserRole.Planner };

            var users = new List<User>
            {
                new User
                {
                    Name = "Willem",
                    LastLoggedIn = DateTime.Now,
                    Role = UserRole.Admin, // Willem is explicitly set to Admin
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("password")
                },
                new User
                {
                    Name = "John Doe",
                    LastLoggedIn = DateTime.Now.AddDays(-1),
                    Role = roles[new Random().Next(roles.Length)], // Random role
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("password")
                },
                new User
                {
                    Name = "Jane Smith",
                    LastLoggedIn = DateTime.Now.AddDays(-2),
                    Role = roles[new Random().Next(roles.Length)],
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("password")
                },
                new User
                {
                    Name = "Bob Johnson",
                    LastLoggedIn = DateTime.Now.AddDays(-3),
                    Role = roles[new Random().Next(roles.Length)],
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("password")
                },
                new User
                {
                    Name = "Alice Brown",
                    LastLoggedIn = DateTime.Now.AddDays(-4),
                    Role = roles[new Random().Next(roles.Length)],
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("password")
                },
                new User
                {
                    Name = "Charlie Wilson",
                    LastLoggedIn = DateTime.Now.AddDays(-5),
                    Role = roles[new Random().Next(roles.Length)],
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("password")
                },
                new User
                {
                    Name = "Diana Miller",
                    LastLoggedIn = DateTime.Now.AddDays(-6),
                    Role = roles[new Random().Next(roles.Length)],
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("password")
                },
                new User
                {
                    Name = "Edward Davis",
                    LastLoggedIn = DateTime.Now.AddDays(-7),
                    Role = roles[new Random().Next(roles.Length)],
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("password")
                },
                new User
                {
                    Name = "Fiona Clark",
                    LastLoggedIn = DateTime.Now.AddDays(-8),
                    Role = roles[new Random().Next(roles.Length)],
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("password")
                },
                new User
                {
                    Name = "George White",
                    LastLoggedIn = DateTime.Now.AddDays(-9),
                    Role = roles[new Random().Next(roles.Length)],
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("password")
                }
            };

            dbContext.Users.AddRange(users);
            dbContext.SaveChanges();
        }
    }
}