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
        private readonly UserManager<ApplicationUser> _userManager; 

        public HomeController(
            ILogger<HomeController> logger, 
            MealStackDbContext context,
            UserManager<ApplicationUser> userManager) : base(userManager)
        {
            _logger = logger;
            _context = context;
            _userManager = userManager; 
        }

        public async Task<IActionResult> Index(string searchTerm, string searchType = "all", bool matchAllIngredients = true)
        {
            if (!string.IsNullOrEmpty(searchTerm))
            {
                return RedirectToAction("Index", "Recipe", new { searchTerm, searchType, matchAllIngredients });
            }

            ViewData["SearchAction"] = "Index";
            ViewData["SearchTerm"] = "";
            ViewData["SearchType"] = "all";
            ViewData["MatchAllIngredients"] = "true";

            // Get categories for navigation and showcase
            ViewBag.Categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();

            // Select hero image
            Random r = new Random();
            ViewBag.HeroImage = r.Next(0, 2) == 0 
                ? "/images/heroes/HeroBanner.jpg" 
                : "/images/heroes/HeroBanner2.jpg";

            // Get latest recipes
            var latestRecipes = await _context.Recipes
                .Include(r => r.CreatedBy)
                .Include(r => r.RecipeCategories).ThenInclude(rc => rc.Category)
                .Include(r => r.Ratings)
                .OrderByDescending(r => r.CreatedDate)
                .Take(3).ToListAsync();

            // Get favorite recipes for logged in user
            if (User.Identity.IsAuthenticated)
            {
                var userId = _userManager.GetUserId(User);
                ViewBag.FavoriteRecipes = await _context.UserFavorites.Where(uf => uf.UserId == userId)
                    .Select(uf => uf.RecipeId).ToListAsync();
            }

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