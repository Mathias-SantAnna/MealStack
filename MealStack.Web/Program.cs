using System;
using System.Collections.Generic;
using System.Globalization;
using MealStack.Infrastructure.Data;
using MealStack.Infrastructure.Data.Entities;
using MealStack.Web.Services;
using MealStack.Web.Services.Interface;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// Allow Npgsql to accept UTC dates without "Kind=Unspecified" errors
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);
IConfiguration Configuration = builder.Configuration;

// 1. EF Core + PostgreSQL
builder.Services.AddDbContext<MealStackDbContext>(options =>
    options.UseNpgsql(
            Configuration.GetConnectionString("DefaultConnection"),
            x => x.MigrationsAssembly("MealStack.Infrastructure")
        )
        .EnableSensitiveDataLogging()
        .LogTo(Console.WriteLine, LogLevel.Information)
);

// 2. Application services
builder.Services.AddScoped<IMealPlanService, MealPlanService>();

// 3. Identity Configuration - FIXED
builder.Services.AddDefaultIdentity<ApplicationUser>(opts =>
    {
        opts.SignIn.RequireConfirmedAccount    = false;
        opts.User.RequireUniqueEmail           = true;
        opts.Password.RequiredLength           = 8;
        opts.Password.RequireDigit             = true;
        opts.Password.RequireLowercase         = true;
        opts.Password.RequireUppercase         = true;
        opts.Password.RequireNonAlphanumeric   = true;
    })
    .AddRoles<IdentityRole>() // Add this line for role support
    .AddEntityFrameworkStores<MealStackDbContext>()
    .AddDefaultTokenProviders()
    .AddDefaultUI();

builder.Services.ConfigureApplicationCookie(opts =>
{
    opts.LoginPath        = "/Identity/Account/Login";
    opts.AccessDeniedPath = "/Identity/Account/AccessDenied";
});

// 4. Localization: en-GB default
var gbCulture = new CultureInfo("en-GB");
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.DefaultRequestCulture   = new RequestCulture(gbCulture);
    options.SupportedCultures       = new List<CultureInfo> { gbCulture };
    options.SupportedUICultures     = new List<CultureInfo> { gbCulture };
});

// 5. MVC & Razor Pages
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

var app = builder.Build();

// 6. Auto-migrate & seed roles/admin
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var db       = services.GetRequiredService<MealStackDbContext>();
    db.Database.Migrate();

    var logger = services.GetRequiredService<ILogger<Program>>();
    try
    {
        var rm = services.GetRequiredService<RoleManager<IdentityRole>>();
        var um = services.GetRequiredService<UserManager<ApplicationUser>>();
        
        // Create roles if they don't exist
        foreach (var role in new[] { "Admin", "User", "Guest" })
        {
            if (!await rm.RoleExistsAsync(role))
            {
                await rm.CreateAsync(new IdentityRole(role));
                logger.LogInformation("Created role: {Role}", role);
            }
        }

        // Create admin user if doesn't exist
        var adminEmail = "admin@mealstack.com";
        var adminUser = await um.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            var admin = new ApplicationUser
            {
                UserName       = "admin",
                Email          = adminEmail,
                EmailConfirmed = true
            };
            var result = await um.CreateAsync(admin, "Admin@123");
            if (result.Succeeded)
            {
                await um.AddToRoleAsync(admin, "Admin");
                logger.LogInformation("Created admin user: {Email}", adminEmail);
            }
            else
            {
                logger.LogError("Failed to create admin user: {Errors}", 
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
        else
        {
            // Ensure existing admin has Admin role
            if (!await um.IsInRoleAsync(adminUser, "Admin"))
            {
                await um.AddToRoleAsync(adminUser, "Admin");
                logger.LogInformation("Added Admin role to existing user: {Email}", adminEmail);
            }
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error during role/admin seeding");
    }
}

// 7. Middleware pipeline
var locOpts = app.Services
    .GetRequiredService<Microsoft.Extensions.Options.IOptions<RequestLocalizationOptions>>()
    .Value;
app.UseRequestLocalization(locOpts);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// 8. Endpoints
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
);
app.MapRazorPages();

app.Run();