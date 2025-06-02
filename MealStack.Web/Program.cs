using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using MealStack.Infrastructure.Data;
using MealStack.Infrastructure.Data.Entities;
using MealStack.Web.Services;
using MealStack.Web.Services.Interface;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;

// Enable legacy timestamp behavior for Npgsql
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// Get connection string from configuration
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// Database Configuration
builder.Services.AddDbContext<MealStackDbContext>(options =>
{
    options.UseNpgsql(connectionString);
    
    // Only enable detailed logging in development
    if (builder.Environment.IsDevelopment())
    {
        options.LogTo(Console.WriteLine, LogLevel.Information);
        options.EnableSensitiveDataLogging();
    }
});

// Application Services
builder.Services.AddScoped<IMealPlanService, MealPlanService>();
builder.Services.AddTransient<IEmailSender, EmailSender>();

// Identity Configuration
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Sign-in requirements
    options.SignIn.RequireConfirmedAccount = false;
    options.SignIn.RequireConfirmedEmail = false;
    
    // User requirements
    options.User.RequireUniqueEmail = true;
    options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
    
    // Password requirements (relaxed for development)
    options.Password.RequiredLength = 6;
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    
    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 10;
    options.Lockout.AllowedForNewUsers = false;
})
.AddEntityFrameworkStores<MealStackDbContext>()
.AddDefaultTokenProviders();

// Authentication Cookie Configuration
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/Login";
    options.ReturnUrlParameter = "returnUrl";
    
    options.ExpireTimeSpan = TimeSpan.FromDays(30);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.Name = "MealStack.Auth";
});

// Localization (GB format for dates)
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var gbCulture = new CultureInfo("en-GB");
    options.DefaultRequestCulture = new RequestCulture(gbCulture);
    options.SupportedCultures = new List<CultureInfo> { gbCulture };
    options.SupportedUICultures = new List<CultureInfo> { gbCulture };
});

// MVC with JSON configuration for AJAX endpoints
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.WriteIndented = builder.Environment.IsDevelopment();
    });

// Session support for shopping lists and temporary data
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

// Anti-forgery token configuration
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

var app = builder.Build();

// Database initialization and seeding
await InitializeDatabaseAsync(app);

// Request localization
var localizationOptions = app.Services
    .GetRequiredService<Microsoft.Extensions.Options.IOptions<RequestLocalizationOptions>>()
    .Value;
app.UseRequestLocalization(localizationOptions);

// Development vs Production middleware
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    LogStartupInfo(app);
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Middleware pipeline
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

// Route configuration
app.MapControllerRoute(
    name: "admin",
    pattern: "Admin/{action=Index}/{id?}",
    defaults: new { controller = "Admin" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

// Helper methods
static async Task InitializeDatabaseAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    
    try
    {
        // Apply migrations
        var dbContext = services.GetRequiredService<MealStackDbContext>();
        await dbContext.Database.MigrateAsync();
        logger.LogInformation("Database migration completed successfully");

        // Initialize roles and users
        await SeedRolesAndUsersAsync(services, logger);
        logger.LogInformation("Database seeding completed successfully");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while initializing the database");
        throw;
    }
}

static async Task SeedRolesAndUsersAsync(IServiceProvider services, ILogger logger)
{
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
    
    // Create roles
    var roles = new[] { "Admin", "User" };
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
            logger.LogInformation("Created role: {Role}", role);
        }
    }

    // Create admin user
    await CreateUserIfNotExistsAsync(userManager, logger, 
        email: "admin@mealstack.com",
        username: "Admin",
        password: "admin123",
        role: "Admin");

    // Create test user
    await CreateUserIfNotExistsAsync(userManager, logger,
        email: "test@mealstack.com", 
        username: "testuser",
        password: "test123",
        role: "User");
}

static async Task CreateUserIfNotExistsAsync(UserManager<ApplicationUser> userManager, 
    ILogger logger, string email, string username, string password, string role)
{
    var existingUser = await userManager.FindByEmailAsync(email);
    if (existingUser != null)
    {
        // Ensure email is confirmed for existing users
        if (!existingUser.EmailConfirmed)
        {
            existingUser.EmailConfirmed = true;
            await userManager.UpdateAsync(existingUser);
            logger.LogInformation("Updated email confirmation for user: {Email}", email);
        }
        return;
    }

    var user = new ApplicationUser
    {
        UserName = username,
        Email = email,
        EmailConfirmed = true,
        ThemePreference = "light"
    };

    var result = await userManager.CreateAsync(user, password);
    if (result.Succeeded)
    {
        await userManager.AddToRoleAsync(user, role);
        logger.LogInformation("Created {Role} user: {Email}", role, email);
    }
    else
    {
        logger.LogError("Failed to create user {Email}: {Errors}", 
            email, string.Join(", ", result.Errors.Select(e => e.Description)));
    }
}

static void LogStartupInfo(WebApplication app)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    var urls = app.Configuration["ASPNETCORE_URLS"] ?? "http://localhost:5261";
    
    logger.LogInformation("🚀 MealStack Development Server Started!");
    logger.LogInformation("📱 Application URL: {Urls}", urls);
    logger.LogInformation("👨‍💼 Admin Login: admin@mealstack.com / admin123");
    logger.LogInformation("👤 Test User: test@mealstack.com / test123");
    logger.LogInformation("🔗 Login: {Urls}/Account/Login", urls);
    logger.LogInformation("🔗 Register: {Urls}/Account/Register", urls);
    logger.LogInformation("⚡ Features: Recipe Management, Meal Planning, Admin Dashboard");
}

public partial class Program { }