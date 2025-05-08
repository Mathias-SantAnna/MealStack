using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using MealStack.Infrastructure.Data;
using MealStack.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using MealStack.Web.Models;

namespace MealStack.Web.Controllers
{
    public class HomeController : BaseController
    {
        private readonly ILogger<HomeController> _logger;
        private readonly MealStackDbContext _context;

        public HomeController(
            ILogger<HomeController> logger, 
            MealStackDbContext context,
            UserManager<ApplicationUser> userManager) : base(userManager)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index(string searchTerm, string searchType = "all", bool matchAllIngredients = true)
        {
            // If search is performed, redirect to Recipe/Index
            if (!string.IsNullOrEmpty(searchTerm))
            {
                return RedirectToAction("Index", "Recipe", new { 
                    searchTerm, 
                    searchType,
                    matchAllIngredients
                });
            }
            
            // Add these lines to fix the null reference errors
            ViewData["SearchAction"] = "Index"; 
            ViewData["SearchTerm"] = "";
            ViewData["SearchType"] = "all";
            ViewData["MatchAllIngredients"] = "true";
            ViewBag.SelectedCategoryId = null;
    
            // Get categories for the navigation menu
            ViewBag.Categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
            
            // Get the 3 most recent recipes
            var latestRecipes = await _context.Recipes
                .Include(r => r.CreatedBy)
                .Include(r => r.RecipeCategories)
                .ThenInclude(rc => rc.Category)
                .OrderByDescending(r => r.CreatedDate)
                .Take(3)
                .ToListAsync();
            
            return View(latestRecipes);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}