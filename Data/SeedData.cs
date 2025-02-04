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

            var users = new List<User>
            {
                new User
                {
                    Name = "Willem",
                    IsAdmin = true,
                    LastLoggedIn = DateTime.Now
                },
                new User
                {
                    Name = "John Doe",
                    LastLoggedIn = DateTime.Now.AddDays(-1)
                },
                new User
                {
                    Name = "Jane Smith",
                    LastLoggedIn = DateTime.Now.AddDays(-2)
                },
                new User
                {
                    Name = "Bob Johnson",
                    LastLoggedIn = DateTime.Now.AddDays(-3)
                },
                new User
                {
                    Name = "Alice Brown",
                    LastLoggedIn = DateTime.Now.AddDays(-4)
                },
                new User
                {
                    Name = "Charlie Wilson",
                    LastLoggedIn = DateTime.Now.AddDays(-5)
                },
                new User
                {
                    Name = "Diana Miller",
                    LastLoggedIn = DateTime.Now.AddDays(-6)
                },
                new User
                {
                    Name = "Edward Davis",
                    LastLoggedIn = DateTime.Now.AddDays(-7)
                },
                new User
                {
                    Name = "Fiona Clark",
                    LastLoggedIn = DateTime.Now.AddDays(-8)
                },
                new User
                {
                    Name = "George White",
                    LastLoggedIn = DateTime.Now.AddDays(-9)
                }
            };

            dbContext.Users.AddRange(users);
            dbContext.SaveChanges();
        }
    }
}
