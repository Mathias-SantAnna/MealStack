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

// Allow Npgsql to accept UTC dates without “Kind=Unspecified” errors
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

// 2. Identity & Application services
builder.Services.AddScoped<IMealPlanService, MealPlanService>();

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(opts =>
    {
        opts.SignIn.RequireConfirmedAccount    = false;
        opts.User.RequireUniqueEmail           = true;
        opts.Password.RequiredLength           = 8;
        opts.Password.RequireDigit             = true;
        opts.Password.RequireLowercase         = true;
        opts.Password.RequireUppercase         = true;
        opts.Password.RequireNonAlphanumeric   = true;
    })
    .AddEntityFrameworkStores<MealStackDbContext>()
    .AddDefaultTokenProviders()
    .AddDefaultUI();

builder.Services.ConfigureApplicationCookie(opts =>
{
    opts.LoginPath        = "/Identity/Account/Login";
    opts.AccessDeniedPath = "/Identity/Account/AccessDenied";
});

// 3. Localization: en-GB default
var gbCulture = new CultureInfo("en-GB");
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.DefaultRequestCulture   = new RequestCulture(gbCulture);
    options.SupportedCultures       = new List<CultureInfo> { gbCulture };
    options.SupportedUICultures     = new List<CultureInfo> { gbCulture };
});

// 4. MVC & Razor Pages
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

var app = builder.Build();

// 5. Auto-migrate & seed roles/admin
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
        foreach (var role in new[] { "Admin", "User", "Guest" })
            if (!await rm.RoleExistsAsync(role))
                await rm.CreateAsync(new IdentityRole(role));

        var adminEmail = "admin@mealstack.com";
        if (await um.FindByEmailAsync(adminEmail) == null)
        {
            var admin = new ApplicationUser
            {
                UserName       = adminEmail,
                Email          = adminEmail,
                EmailConfirmed = true
            };
            var res = await um.CreateAsync(admin, "Admin@123");
            if (res.Succeeded)
                await um.AddToRoleAsync(admin, "Admin");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Seeding roles/admin failed");
    }
}

// 6. Middleware pipeline - Apply en-GB culture to each request
var locOpts = app.Services
    .GetRequiredService<Microsoft.Extensions.Options.IOptions<RequestLocalizationOptions>>()
    .Value;
app.UseRequestLocalization(locOpts);

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// 7. Endpoints
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
);
app.MapRazorPages();

app.Run();
