using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System.Threading;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;
using TestProject.Models;

namespace TestProject.Data
{
    public class SeedDataService : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;

        public SeedDataService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
{
    using (var scope = _serviceProvider.CreateScope())
    {
        var scopedServices = scope.ServiceProvider;
        var dbContext = scopedServices.GetRequiredService<ApplicationDbContext>();
        var userManager = scopedServices.GetRequiredService<UserManager<User>>();
        
        // Seed HeavyProducts
        SeedData.Initialize(dbContext);
        
        // Seed Users
        await dbContext.SeedUsersAsync(userManager);
    }
}
        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}