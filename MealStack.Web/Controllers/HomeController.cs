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

        public async Task<IActionResult> Index(string searchTerm = null, string searchType = "all", 
            string difficulty = null, string createdBy = null, bool matchAllIngredients = true)
        {
            return await TryExecuteAsync(async () =>
            {
                if (!string.IsNullOrEmpty(searchTerm) || !string.IsNullOrEmpty(difficulty) || !string.IsNullOrEmpty(createdBy))
                {
                    return RedirectToAction("Index", "Recipe", new { searchTerm, searchType, difficulty, createdBy, matchAllIngredients });
                }

                ViewData["SearchAction"] = "Index";
                ViewData["SearchTerm"] = "";
                ViewData["SearchType"] = "all";
                ViewData["MatchAllIngredients"] = "true";
                ViewData["CreatedBy"] = "";

                ViewBag.Categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
                
                ViewBag.Authors = await _context.Recipes
                    .Include(r => r.CreatedBy)
                    .Where(r => r.CreatedBy != null)
                    .Select(r => new { Id = r.CreatedById, Name = r.CreatedBy.UserName })
                    .Distinct()
                    .OrderBy(a => a.Name)
                    .ToListAsync();
                
                var r = new Random();
                int idx = r.Next(0, 9);
                ViewBag.HeroImage = $"/images/heroes/HeroBanner{idx}.jpg";
                
                var latestRecipes = await _context.Recipes
                    .Include(r => r.CreatedBy)
                    .Include(r => r.RecipeCategories).ThenInclude(rc => rc.Category)
                    .Include(r => r.Ratings)
                    .OrderByDescending(r => r.CreatedDate)
                    .Take(3).ToListAsync();
                
                if (User.Identity.IsAuthenticated)
                {
                    var userId = GetUserId(); 
                    ViewBag.FavoriteRecipes = await _context.UserFavorites.Where(uf => uf.UserId == userId)
                        .Select(uf => uf.RecipeId).ToListAsync();
                }

                return View(latestRecipes);
            }, "Error loading home page. Please try again later.");
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