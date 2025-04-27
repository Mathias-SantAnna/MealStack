using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MealStack.Infrastructure.Data;
using MealStack.Infrastructure.Data.Entities;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace MealStack.Web.Controllers
{
    public class RecipeController : BaseController
    {
        private readonly MealStackDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public RecipeController(
            MealStackDbContext context, 
            UserManager<ApplicationUser> userManager) 
            : base(userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Recipe
        public async Task<IActionResult> Index(string searchTerm, string difficulty, string timeFilter, string sortBy, int? categoryId)
        {
            var recipesQuery = _context.Recipes
                .Include(r => r.CreatedBy)
                .Include(r => r.RecipeCategories)
                .ThenInclude(rc => rc.Category)
                .AsQueryable();
            
            // Apply search filter
            if (!string.IsNullOrEmpty(searchTerm))
            {
                recipesQuery = recipesQuery.Where(r => 
                    r.Title.Contains(searchTerm) || 
                    r.Description.Contains(searchTerm) ||
                    r.Ingredients.Contains(searchTerm));
            }
            
            // Apply difficulty filter
            if (!string.IsNullOrEmpty(difficulty))
            {
                if (Enum.TryParse<DifficultyLevel>(difficulty, out var difficultyLevel))
                {
                    recipesQuery = recipesQuery.Where(r => r.Difficulty == difficultyLevel);
                }
            }
            
            // Apply time filter
            if (!string.IsNullOrEmpty(timeFilter) && int.TryParse(timeFilter, out var minutes))
            {
                recipesQuery = recipesQuery.Where(r => (r.PrepTimeMinutes + r.CookTimeMinutes) <= minutes);
            }
            
            // Apply category filter
            if (categoryId.HasValue)
            {
                recipesQuery = recipesQuery.Where(r => r.RecipeCategories.Any(rc => rc.CategoryId == categoryId.Value));
            }
            
            // Apply sorting
            switch (sortBy)
            {
                case "oldest":
                    recipesQuery = recipesQuery.OrderBy(r => r.CreatedDate);
                    break;
                case "fastest":
                    recipesQuery = recipesQuery.OrderBy(r => r.PrepTimeMinutes + r.CookTimeMinutes);
                    break;
                case "newest":
                default:
                    recipesQuery = recipesQuery.OrderByDescending(r => r.CreatedDate);
                    break;
            }
            
            // Get categories for filter buttons
            ViewBag.Categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
            ViewBag.SelectedCategoryId = categoryId;
            
            // Store filter values for the view
            ViewData["SearchTerm"] = searchTerm;
            ViewData["Difficulty"] = difficulty;
            ViewData["TimeFilter"] = timeFilter;
            ViewData["SortBy"] = sortBy ?? "newest";
            
            var recipes = await recipesQuery.ToListAsync();
            return View(recipes);
        }

        // GET: Recipe/MyRecipes
        [Authorize]
        public async Task<IActionResult> MyRecipes(string searchTerm, string difficulty, string timeFilter, string sortBy, int? categoryId)
        {
            var userId = _userManager.GetUserId(User);
            var recipesQuery = _context.Recipes
                .Include(r => r.CreatedBy)
                .Include(r => r.RecipeCategories)
                .ThenInclude(rc => rc.Category)
                .Where(r => r.CreatedById == userId)
                .AsQueryable();
            
            // Apply search filter
            if (!string.IsNullOrEmpty(searchTerm))
            {
                recipesQuery = recipesQuery.Where(r => 
                    r.Title.Contains(searchTerm) || 
                    r.Description.Contains(searchTerm) ||
                    r.Ingredients.Contains(searchTerm));
            }
            
            // Apply difficulty filter
            if (!string.IsNullOrEmpty(difficulty))
            {
                if (Enum.TryParse<DifficultyLevel>(difficulty, out var difficultyLevel))
                {
                    recipesQuery = recipesQuery.Where(r => r.Difficulty == difficultyLevel);
                }
            }
            
            // Apply time filter
            if (!string.IsNullOrEmpty(timeFilter) && int.TryParse(timeFilter, out var minutes))
            {
                recipesQuery = recipesQuery.Where(r => (r.PrepTimeMinutes + r.CookTimeMinutes) <= minutes);
            }
            
            // Apply category filter
            if (categoryId.HasValue)
            {
                recipesQuery = recipesQuery.Where(r => r.RecipeCategories.Any(rc => rc.CategoryId == categoryId.Value));
            }
            
            // Apply sorting
            switch (sortBy)
            {
                case "oldest":
                    recipesQuery = recipesQuery.OrderBy(r => r.CreatedDate);
                    break;
                case "fastest":
                    recipesQuery = recipesQuery.OrderBy(r => r.PrepTimeMinutes + r.CookTimeMinutes);
                    break;
                case "newest":
                default:
                    recipesQuery = recipesQuery.OrderByDescending(r => r.CreatedDate);
                    break;
            }
            
            // Get categories for filter buttons
            ViewBag.Categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
            ViewBag.SelectedCategoryId = categoryId;
            
            // Store filter values for the view
            ViewData["SearchTerm"] = searchTerm;
            ViewData["Difficulty"] = difficulty;
            ViewData["TimeFilter"] = timeFilter;
            ViewData["SortBy"] = sortBy ?? "newest";
            
            var recipes = await recipesQuery.ToListAsync();
            return View(recipes);
        }

        // GET: Recipe/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var recipe = await _context.Recipes
                .Include(r => r.CreatedBy)
                .Include(r => r.RecipeCategories)
                .ThenInclude(rc => rc.Category)
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
            
            // Get categories for dropdown
            ViewBag.Categories = _context.Categories.OrderBy(c => c.Name).ToList();
            
            return View(recipe);
        }

        // POST: Recipe/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Create(RecipeEntity recipe, int[] selectedCategories)
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
                    
                    // Add categories
                    if (selectedCategories != null && selectedCategories.Length > 0)
                    {
                        foreach (var categoryId in selectedCategories)
                        {
                            _context.Add(new RecipeCategoryEntity
                            {
                                RecipeId = recipe.Id,
                                CategoryId = categoryId
                            });
                        }
                        await _context.SaveChangesAsync();
                    }
                    
                    TempData["Message"] = "Recipe created successfully!";
                    return RedirectToAction("MyRecipes");
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"An error occurred: {ex.Message}");
            }
            
            // If we got this far, something failed, redisplay form
            ViewBag.Categories = _context.Categories.OrderBy(c => c.Name).ToList();
            return View(recipe);
        }

        // GET: Recipe/Edit/5
        [Authorize]
        public async Task<IActionResult> Edit(int id)
        {
            var recipe = await _context.Recipes
                .Include(r => r.RecipeCategories)
                .FirstOrDefaultAsync(r => r.Id == id);
                
            if (recipe == null)
            {
                return NotFound();
            }

            // Only allow edit if user created the recipe or is admin
            if (recipe.CreatedById == _userManager.GetUserId(User) || User.IsInRole("Admin"))
            {
                // Get categories for dropdown and mark the selected ones
                ViewBag.Categories = _context.Categories.OrderBy(c => c.Name).ToList();
                ViewBag.SelectedCategories = recipe.RecipeCategories?.Select(rc => rc.CategoryId).ToList() ?? new List<int>();
                
                return View(recipe);
            }
            
            return Forbid();
        }

        // POST: Recipe/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Edit(int id, RecipeEntity recipe, int[] selectedCategories)
        {
            if (id != recipe.Id)
            {
                return NotFound();
            }

            var existingRecipe = await _context.Recipes
                .Include(r => r.RecipeCategories)
                .FirstOrDefaultAsync(r => r.Id == id);
                
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
                        
                        // Update categories
                        // First, remove existing categories
                        var existingCategories = _context.RecipeCategories
                            .Where(rc => rc.RecipeId == id);
                        _context.RemoveRange(existingCategories);
                        
                        // Then add selected categories
                        if (selectedCategories != null && selectedCategories.Length > 0)
                        {
                            foreach (var categoryId in selectedCategories)
                            {
                                _context.Add(new RecipeCategoryEntity
                                {
                                    RecipeId = recipe.Id,
                                    CategoryId = categoryId
                                });
                            }
                        }
                        
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
                
                // If we got this far, something failed, redisplay form
                ViewBag.Categories = _context.Categories.OrderBy(c => c.Name).ToList();
                ViewBag.SelectedCategories = existingRecipe.RecipeCategories?.Select(rc => rc.CategoryId).ToList() ?? new List<int>();
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