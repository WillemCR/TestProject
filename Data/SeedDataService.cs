using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System.Threading;
using System.Threading.Tasks;

namespace TestProject.Data
{
    public class SeedDataService : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;

        public SeedDataService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var scopedServices = scope.ServiceProvider;
                var dbContext = scopedServices.GetRequiredService<ApplicationDbContext>();
                dbContext.SeedUsers();
            }

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}