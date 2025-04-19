using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MealStack.Infrastructure.Data;
using MealStack.Infrastructure.Data.Entities;
using System;
using System.Linq;
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
            // Return all recipes for the Index view
            var recipes = await _context.Recipes
                .Include(r => r.CreatedBy)
                .OrderByDescending(r => r.CreatedDate)
                .ToListAsync();
            return View(recipes);
        }

        // GET: Recipe/MyRecipes
        [Authorize]
        public async Task<IActionResult> MyRecipes()
        {
            var userId = _userManager.GetUserId(User);
            var recipes = await _context.Recipes
                .Include(r => r.CreatedBy)
                .Where(r => r.CreatedById == userId)
                .OrderByDescending(r => r.CreatedDate)
                .ToListAsync();
            return View(recipes);
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

            return View(recipe);
        }

        // GET: Recipe/Create
        [Authorize]
        public IActionResult Create()
        {
            var recipe = new RecipeEntity
            {
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
            try
            {
                // Set required fields BEFORE ModelState.IsValid check
                recipe.CreatedById = _userManager.GetUserId(User);
                recipe.CreatedDate = DateTime.UtcNow;
                
                // Clear any model errors for CreatedById since we just set it
                ModelState.Remove("CreatedById");
                
                if (ModelState.IsValid)
                {
                    _context.Recipes.Add(recipe);
                    await _context.SaveChangesAsync();
                    
                    TempData["Message"] = "Recipe created successfully!";
                    return RedirectToAction("MyRecipes");
                }
                else
                {
                    foreach (var state in ModelState)
                    {
                        foreach (var error in state.Value.Errors)
                        {
                            Console.WriteLine($"Model error for {state.Key}: {error.ErrorMessage}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating recipe: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                
                ModelState.AddModelError("", $"An error occurred: {ex.Message}");
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
        public async Task<IActionResult> Edit(int id, RecipeEntity recipe)
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
                        // Update properties
                        existingRecipe.Title = recipe.Title;
                        existingRecipe.Description = recipe.Description;
                        existingRecipe.Instructions = recipe.Instructions;
                        existingRecipe.Ingredients = recipe.Ingredients;
                        existingRecipe.PrepTimeMinutes = recipe.PrepTimeMinutes;
                        existingRecipe.CookTimeMinutes = recipe.CookTimeMinutes;
                        existingRecipe.Servings = recipe.Servings;
                        existingRecipe.Difficulty = recipe.Difficulty;
                        existingRecipe.UpdatedDate = DateTime.UtcNow;
                        
                        _context.Update(existingRecipe);
                        await _context.SaveChangesAsync();
                        
                        TempData["Message"] = "Recipe updated successfully!";
                        return RedirectToAction("MyRecipes");
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
                }
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
                
                TempData["Message"] = "Recipe deleted successfully!";
                return RedirectToAction("MyRecipes");
            }
            
            return Forbid();
        }

        private bool RecipeExists(int id)
        {
            return _context.Recipes.Any(e => e.Id == id);
        }
    }
}