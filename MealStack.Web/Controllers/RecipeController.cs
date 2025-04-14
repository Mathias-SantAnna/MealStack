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
            var recipes = await _context.Recipes
                .Include(r => r.CreatedBy)
                .ToListAsync();
            
            return View(recipes);
        }

        // GET: Recipe/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var recipe = await _context.Recipes
                .Include(r => r.CreatedBy)
                .FirstOrDefaultAsync(m => m.Id == id);
                
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
        public async Task<IActionResult> Create([Bind("Title,Description,Ingredients,Instructions,PrepTimeMinutes,CookTimeMinutes,Difficulty,Servings")] RecipeEntity recipe)
        {
            if (ModelState.IsValid)
            {
                recipe.CreatedById = _userManager.GetUserId(User);
                recipe.CreatedDate = DateTime.UtcNow;
                
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
            
            var currentUserId = _userManager.GetUserId(User);
            if (recipe.CreatedById != currentUserId && !User.IsInRole("Admin"))
            {
                return Forbid();
            }
            
            return View(recipe);
        }

        // POST: Recipe/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Description,Ingredients,Instructions,PrepTimeMinutes,CookTimeMinutes,Difficulty,Servings")] RecipeEntity recipe)
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

            var currentUserId = _userManager.GetUserId(User);
            if (existingRecipe.CreatedById != currentUserId && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    existingRecipe.Title = recipe.Title;
                    existingRecipe.Description = recipe.Description;
                    existingRecipe.Ingredients = recipe.Ingredients;
                    existingRecipe.Instructions = recipe.Instructions;
                    existingRecipe.PrepTimeMinutes = recipe.PrepTimeMinutes;
                    existingRecipe.CookTimeMinutes = recipe.CookTimeMinutes;
                    existingRecipe.Difficulty = recipe.Difficulty;
                    existingRecipe.Servings = recipe.Servings;
                    existingRecipe.UpdatedDate = DateTime.UtcNow;
                    
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

        // GET: Recipe/Delete/5
        [Authorize]
        public async Task<IActionResult> Delete(int id)
        {
            var recipe = await _context.Recipes
                .Include(r => r.CreatedBy)
                .FirstOrDefaultAsync(m => m.Id == id);
                
            if (recipe == null)
            {
                return NotFound();
            }

            var currentUserId = _userManager.GetUserId(User);
            if (recipe.CreatedById != currentUserId && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            return View(recipe);
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

            var currentUserId = _userManager.GetUserId(User);
            if (recipe.CreatedById != currentUserId && !User.IsInRole("Admin"))
            {
                return Forbid();
            }
            
            _context.Recipes.Remove(recipe);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool RecipeExists(int id)
        {
            return _context.Recipes.Any(e => e.Id == id);
        }
    }
}