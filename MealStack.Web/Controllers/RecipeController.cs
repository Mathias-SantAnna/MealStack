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

        // GET: Recipe
        public async Task<IActionResult> Index()
        {
            var recipes = await _context.Recipes.Include(r => r.CreatedBy).ToListAsync();
            return View(model: recipes);
        }

        // GET: Recipe/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var recipe = await _context.Recipes
                .Include(r => r.CreatedBy)
                .FirstOrDefaultAsync(r => r.Id == id);
                
            if (recipe == null)
            {
                return NotFound();
            }

            return View(model: recipe);
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
            return View(model: recipe);
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
            var recipe = await _context.Recipes
                .Include(r => r.CreatedBy)
                .FirstOrDefaultAsync(r => r.Id == id);
                
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
    }
}