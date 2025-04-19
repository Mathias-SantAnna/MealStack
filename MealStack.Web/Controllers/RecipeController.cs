using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MealStack.Infrastructure.Data;
using MealStack.Infrastructure.Data.Entities;
using System.Threading.Tasks;
using System.Linq;

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

        // GET: Recipe
        public async Task<IActionResult> Index()
        {
            // If user is logged in, show their recipes
            if (User.Identity.IsAuthenticated)
            {
                string userId = _userManager.GetUserId(User);
                var userRecipes = await _context.Recipes
                    .Where(r => r.CreatedById == userId)
                    .ToListAsync();
                return View(userRecipes);
            }
            
            // Otherwise, show all public recipes
            var recipes = await _context.Recipes.ToListAsync();
            return View(recipes);
        }
        
        // GET: Recipe/Community
        public async Task<IActionResult> Community()
        {
            // Show all public recipes
            var recipes = await _context.Recipes.ToListAsync();
            return View(recipes);
        }

        // GET: Recipe/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var recipe = await _context.Recipes.FindAsync(id);
            if (recipe == null)
            {
                return NotFound();
            }

            return View(recipe);
        }

        // GET: Recipe/Create
        [Authorize]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Recipe/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Create([Bind("Title,Description,Instructions,PrepTimeMinutes,CookTimeMinutes,Servings,Difficulty,Ingredients")] RecipeEntity recipe)
        {
            if (ModelState.IsValid)
            {
                recipe.CreatedById = _userManager.GetUserId(User);
                recipe.CreatedDate = System.DateTime.UtcNow;
                
                _context.Add(recipe);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(recipe);
        }

        // GET: Recipe/Edit/5
        [Authorize]
        public async Task<IActionResult> Edit(int id)
        {
            var recipe = await _context.Recipes.FindAsync(id);
            if (recipe == null)
            {
                return NotFound();
            }

            // Only allow edit if user created the recipe or is admin
            if (recipe.CreatedById == _userManager.GetUserId(User) || User.IsInRole("Admin"))
            {
                return View(recipe);
            }
            
            return Forbid();
        }

        // POST: Recipe/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Description,Instructions,PrepTimeMinutes,CookTimeMinutes,Servings,Difficulty,Ingredients")] RecipeEntity recipe)
        {
            if (id != recipe.Id)
            {
                return NotFound();
            }

            var existingRecipe = await _context.Recipes.FindAsync(id);
            if (existingRecipe == null)
            {
                return NotFound();
            }

            // Only allow edit if user created the recipe or is admin
            if (existingRecipe.CreatedById == _userManager.GetUserId(User) || User.IsInRole("Admin"))
            {
                if (ModelState.IsValid)
                {
                    try
                    {
                        existingRecipe.Title = recipe.Title;
                        existingRecipe.Description = recipe.Description;
                        existingRecipe.Instructions = recipe.Instructions;
                        existingRecipe.PrepTimeMinutes = recipe.PrepTimeMinutes;
                        existingRecipe.CookTimeMinutes = recipe.CookTimeMinutes;
                        existingRecipe.Servings = recipe.Servings;
                        existingRecipe.Difficulty = recipe.Difficulty;
                        existingRecipe.Ingredients = recipe.Ingredients;
                        existingRecipe.UpdatedDate = System.DateTime.UtcNow;
                        
                        await _context.SaveChangesAsync();
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        if (!RecipeExists(recipe.Id))
                        {
                            return NotFound();
                        }
                        else
                        {
                            throw;
                        }
                    }
                    return RedirectToAction(nameof(Index));
                }
                return View(recipe);
            }
            
            return Forbid();
        }

        // GET: Recipe/Delete/5
        [Authorize]
        public async Task<IActionResult> Delete(int id)
        {
            var recipe = await _context.Recipes.FindAsync(id);
            if (recipe == null)
            {
                return NotFound();
            }

            // Only allow delete if user created the recipe or is admin
            if (recipe.CreatedById == _userManager.GetUserId(User) || User.IsInRole("Admin"))
            {
                return View(recipe);
            }
            
            return Forbid();
        }

        // POST: Recipe/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var recipe = await _context.Recipes.FindAsync(id);
            if (recipe == null)
            {
                return NotFound();
            }

            // Only allow delete if user created the recipe or is admin
            if (recipe.CreatedById == _userManager.GetUserId(User) || User.IsInRole("Admin"))
            {
                _context.Recipes.Remove(recipe);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            
            return Forbid();
        }

        private bool RecipeExists(int id)
        {
            return _context.Recipes.Any(e => e.Id == id);
        }
    }
}