using MealStack.Infrastructure.Data;
using MealStack.Infrastructure.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq; 

var builder = WebApplication.CreateBuilder(args);

// 1. Configure EF Core + PostgreSQL with detailed logging
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddDbContext<MealStackDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"),
                x => x.MigrationsAssembly("MealStack.Infrastructure"))
            .EnableSensitiveDataLogging()
            .LogTo(Console.WriteLine, LogLevel.Information));
    
    builder.Logging.AddConsole()
        .AddDebug()
        .SetMinimumLevel(LogLevel.Debug);
}
else
{
    // Production configuration without detailed logging
    builder.Services.AddDbContext<MealStackDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"),
            x => x.MigrationsAssembly("MealStack.Infrastructure")));
}

// 2. Add Identity with Role support - SINGLE REGISTRATION
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        // Combine your desired options here
        options.SignIn.RequireConfirmedAccount = false;
        options.User.RequireUniqueEmail = true;
        options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequireUppercase = true;
        options.Password.RequiredLength = 8;
    })
    .AddEntityFrameworkStores<MealStackDbContext>()
    .AddDefaultTokenProviders();

// 3. Configure cookie login paths
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
});

// 4. Add MVC and Razor Pages
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

var app = builder.Build();

// 5. Seed roles and default admin user (only create if missing)
using (var scope = app.Services.CreateScope())
{
    var services    = scope.ServiceProvider;
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

    // ensure roles exist
    string[] roles = { "Admin", "User", "Guest" };
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));
    }

    // admin credentials
    string adminEmail    = "admin@mealstack.com";
    string adminPassword = "Admin@123";
    string adminUsername = adminEmail;

    // find or create admin user
    var adminUser = await userManager.FindByEmailAsync(adminEmail);
    if (adminUser == null)
    {
        adminUser = new ApplicationUser
        {
            UserName       = adminUsername,
            Email          = adminEmail,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(adminUser, adminPassword);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(adminUser, "Admin");
        }
    }
}

// 6. Middleware pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

// Add a custom error handling middleware
app.Use(async (context, next) =>
{
    try
    {
        await next();
        
        if (context.Response.StatusCode == 404 && !context.Response.HasStarted)
        {
            context.Request.Path = "/Home/NotFound";
            await next();
        }
    }
    catch (Exception ex)
    {
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An unhandled exception occurred");
        
        // Redirect to error page
        context.Request.Path = "/Home/Error";
        await next();
    }
});

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Authentication & Authorization middleware - SINGLE REGISTRATION
app.UseAuthentication();
app.UseAuthorization();

// 7. Routing
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();