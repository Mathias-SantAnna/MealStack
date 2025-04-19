using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MealStack.Infrastructure.Data;
using MealStack.Infrastructure.Data.Entities;
using System.Threading.Tasks;

namespace MealStack.Web.Controllers
{
    public class RecipeController : Controller
    {
        private readonly MealStackDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public RecipeController(MealStackDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Other actions...

        // GET: Recipe/Create
        [Authorize]
        public IActionResult Create()
        {
            // Initialize a new recipe with default values
            var recipe = new RecipeEntity
            {
                // Set default values if needed
                Difficulty = DifficultyLevel.Easy,
                PrepTimeMinutes = 15,
                CookTimeMinutes = 30,
                Servings = 4
            };
            
            return View(recipe);
        }

        // POST: Recipe/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Create(RecipeEntity recipe)
        {
            // Always set the CreatedById before validation
            recipe.CreatedById = _userManager.GetUserId(User);
            
            if (ModelState.IsValid)
            {
                recipe.CreatedDate = System.DateTime.UtcNow;
                
                _context.Add(recipe);
                await _context.SaveChangesAsync();
                
                TempData["Message"] = "Recipe created successfully!";
                return RedirectToAction("MyRecipes");
            }
            
            // If we got this far, something failed; redisplay form
            return View(recipe);
        }
        
    }
}