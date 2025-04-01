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
             if (!context.LosseArtikelen.Any())
    {
        var losseArtikelen = new List<LosseArtikelen>
        {
            new LosseArtikelen { orderid = "O2500144.1", omschrijving = "ZIJKAPSET 45° 180MM 7021", artikelno = "ZK-180-45", lengte = "180", aantal = 2 },
            new LosseArtikelen { orderid = "O2500139.1", omschrijving = "KOPPELSTUK ROND 150MM", artikelno = "KP-150-R", lengte = "150", aantal = 4 },
            new LosseArtikelen { orderid = "O2500131.1", omschrijving = "BOCHT 90° 200MM", artikelno = "B-200-90", lengte = "200", aantal = 1 },
            new LosseArtikelen { orderid = "O2550018.1", omschrijving = "VERLOOPSTUK 250-200MM", artikelno = "VL-250-200", lengte = "300", aantal = 2 },
            new LosseArtikelen { orderid = "O2452250.1", omschrijving = "EINDKAP 160MM", artikelno = "EK-160", lengte = "160", aantal = 3 },
            new LosseArtikelen { orderid = "O2452177.1", omschrijving = "T-STUK 90° 200MM", artikelno = "TS-200-90", lengte = "200", aantal = 1 },
            new LosseArtikelen { orderid = "O2452177.2", omschrijving = "WANDBEUGEL 180MM", artikelno = "WB-180", lengte = "180", aantal = 5 },
            new LosseArtikelen { orderid = "O2550004.1", omschrijving = "DAKDOORVOER 150MM", artikelno = "DD-150", lengte = "150", aantal = 2 },
            new LosseArtikelen { orderid = "O2500267.1", omschrijving = "INSPECTIELUIK 200MM", artikelno = "IL-200", lengte = "200", aantal = 1 },
            new LosseArtikelen { orderid = "O2500267.2", omschrijving = "CONDENSAFVOER 160MM", artikelno = "CA-160", lengte = "160", aantal = 3 },
            new LosseArtikelen { orderid = "O2500126.1", omschrijving = "MUURPLAAT 180MM", artikelno = "MP-180", lengte = "180", aantal = 2 },
            new LosseArtikelen { orderid = "O2500123.1", omschrijving = "ROZET 200MM 7021", artikelno = "RZ-200", lengte = "200", aantal = 4 },
            new LosseArtikelen { orderid = "O2500123.2", omschrijving = "KLEMBAND 150MM", artikelno = "KB-150", lengte = "150", aantal = 6 },
            new LosseArtikelen { orderid = "O2500123.3", omschrijving = "NISBUS 180MM", artikelno = "NB-180", lengte = "180", aantal = 2 },
            new LosseArtikelen { orderid = "O2550011.1", omschrijving = "REGENKAP 200MM", artikelno = "RK-200", lengte = "200", aantal = 1 }
        };

        context.LosseArtikelen.AddRange(losseArtikelen);
        context.SaveChanges();
    }


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
            LastLoggedIn = DateTime.Now,
            Role = UserRole.Admin,
            UserName = "Wim",
            Email = "wvanommen@suncircle.nl"
        }, "pass123"),
       
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