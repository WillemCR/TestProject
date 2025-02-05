using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OfficeOpenXml;
using TestProject.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "Cookies";
    options.DefaultChallengeScheme = "Cookies";
})
.AddCookie("Cookies", options =>
{
    options.LoginPath = "/Login";
    options.LogoutPath = "/Login?handler=Logout";
    options.AccessDeniedPath = "/Login";
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30); // Set cookie to expire in 30 days
    options.SlidingExpiration = true; // Optional: Reset expiration time on each request
});
builder.Services.AddHostedService<SeedDataService>();


// Add services to the container.
builder.Services.AddRazorPages();

// Add database context
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHostedService<SeedDataService>();

builder.Services.AddAntiforgery(options => {
    options.HeaderName = "X-CSRF-TOKEN";
});
// Add EPPlus license context
ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

var app = builder.Build();

// Apply pending migrations
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<ApplicationDbContext>();
    context.Database.Migrate();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.Run();
