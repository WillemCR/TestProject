using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using TestProject.Models;

namespace TestProject.Data
{
    public static class SeedData
    {
        public static void SeedUsers(this ApplicationDbContext dbContext)
        {
            if (dbContext.Users.Any())
            {
                return; // Seed data only if the database is empty.
            }

            // First, seed a single crew
            if (!dbContext.Crews.Any())
            {
                var crew = new Crew
                {
                    Name = "Default Crew",
                };
                dbContext.Crews.Add(crew);
                dbContext.SaveChanges(); // Save the crew to get an ID before adding users
            }

            // Now fetch the crew we just added or the first one if it already exists
            var defaultCrew = dbContext.Crews.First();

            UserRole[] roles = { UserRole.Laadploeg, UserRole.Planner };

            var users = new List<User>
            {
                new User
                {
                    Name = "Willem",
                    LastLoggedIn = DateTime.Now,
                    Role = UserRole.Admin, // Willem is explicitly set to Admin
                    CrewId = defaultCrew.CrewId // Assign to the default crew
                },
                new User
                {
                    Name = "John Doe",
                    LastLoggedIn = DateTime.Now.AddDays(-1),
                    Role = roles[new Random().Next(roles.Length)], // Random role
                    CrewId = defaultCrew.CrewId
                },
                new User
                {
                    Name = "Jane Smith",
                    LastLoggedIn = DateTime.Now.AddDays(-2),
                    Role = roles[new Random().Next(roles.Length)],
                    CrewId = defaultCrew.CrewId
                },
                new User
                {
                    Name = "Bob Johnson",
                    LastLoggedIn = DateTime.Now.AddDays(-3),
                    Role = roles[new Random().Next(roles.Length)],
                    CrewId = defaultCrew.CrewId
                },
                new User
                {
                    Name = "Alice Brown",
                    LastLoggedIn = DateTime.Now.AddDays(-4),
                    Role = roles[new Random().Next(roles.Length)],
                    CrewId = defaultCrew.CrewId
                },
                new User
                {
                    Name = "Charlie Wilson",
                    LastLoggedIn = DateTime.Now.AddDays(-5),
                    Role = roles[new Random().Next(roles.Length)],
                    CrewId = defaultCrew.CrewId
                },
                new User
                {
                    Name = "Diana Miller",
                    LastLoggedIn = DateTime.Now.AddDays(-6),
                    Role = roles[new Random().Next(roles.Length)],
                    CrewId = defaultCrew.CrewId
                },
                new User
                {
                    Name = "Edward Davis",
                    LastLoggedIn = DateTime.Now.AddDays(-7),
                    Role = roles[new Random().Next(roles.Length)],
                    CrewId = defaultCrew.CrewId
                },
                new User
                {
                    Name = "Fiona Clark",
                    LastLoggedIn = DateTime.Now.AddDays(-8),
                    Role = roles[new Random().Next(roles.Length)],
                    CrewId = defaultCrew.CrewId
                },
                new User
                {
                    Name = "George White",
                    LastLoggedIn = DateTime.Now.AddDays(-9),
                    Role = roles[new Random().Next(roles.Length)],
                    CrewId = defaultCrew.CrewId
                }
            };

            dbContext.Users.AddRange(users);
            dbContext.SaveChanges();
        }
    }
}