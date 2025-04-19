using Microsoft.AspNetCore.Mvc;
using MealStack.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using MealStack.Web.Models;

namespace MealStack.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly MealStackDbContext _context;

        public HomeController(ILogger<HomeController> logger, MealStackDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Get the 3 most recent recipes
            var latestRecipes = await _context.Recipes
                .Include(r => r.CreatedBy)
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