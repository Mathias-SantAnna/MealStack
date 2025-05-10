using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MealStack.Infrastructure.Data;
using MealStack.Infrastructure.Data.Entities;
using MealStack.Web.Models;
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
        public async Task<IActionResult> Index(RecipeSearchViewModel searchModel)
        {
            // Save search parameters to ViewData for form repopulation
            ViewData["SearchTerm"] = searchModel.SearchTerm;
            ViewData["SearchType"] = searchModel.SearchType ?? "all";
            ViewData["Difficulty"] = searchModel.Difficulty;
            ViewData["SortBy"] = searchModel.SortBy ?? "newest";
            ViewData["MatchAllIngredients"] = searchModel.MatchAllIngredients.ToString().ToLower();
            ViewData["CategoryId"] = searchModel.CategoryId;
            
            // Create a query for recipes
            var recipesQuery = _context.Recipes
                .Include(r => r.CreatedBy)
                .Include(r => r.RecipeCategories)
                .ThenInclude(rc => rc.Category)
                .AsQueryable();
            
            // Apply search filters based on the search model
            recipesQuery = ApplySearchFilters(recipesQuery, searchModel);
            
            // Get categories for filter buttons
            ViewBag.Categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
            ViewBag.SelectedCategoryId = searchModel.CategoryId;
            ViewData["SearchAction"] = "Index";
            
            // Store search model for the view
            var recipes = await recipesQuery.ToListAsync();
            
            // Add favorites for logged-in users
            if (User.Identity.IsAuthenticated)
            {
                var userId = _userManager.GetUserId(User);
                var favoriteRecipeIds = await _context.UserFavorites
                    .Where(uf => uf.UserId == userId)
                    .Select(uf => uf.RecipeId)
                    .ToListAsync();
            
                ViewBag.FavoriteRecipes = favoriteRecipeIds;
            }
            
            return View(recipes);
        }

        // GET: Recipe/MyRecipes
        [Authorize]
        public async Task<IActionResult> MyRecipes(RecipeSearchViewModel searchModel)
        {
            // Save search parameters to ViewData for form repopulation
            ViewData["SearchTerm"] = searchModel.SearchTerm;
            ViewData["SearchType"] = searchModel.SearchType ?? "all";
            ViewData["Difficulty"] = searchModel.Difficulty;
            ViewData["SortBy"] = searchModel.SortBy ?? "newest";
            ViewData["MatchAllIngredients"] = searchModel.MatchAllIngredients.ToString().ToLower();
            ViewData["CategoryId"] = searchModel.CategoryId;
            
            // Get the current user ID
            var userId = _userManager.GetUserId(User);
            
            // Create a query for the user's recipes
            var recipesQuery = _context.Recipes
                .Include(r => r.CreatedBy)
                .Include(r => r.RecipeCategories)
                .ThenInclude(rc => rc.Category)
                .Where(r => r.CreatedById == userId)
                .AsQueryable();
            
            // Apply search filters - search model
            recipesQuery = ApplySearchFilters(recipesQuery, searchModel);
            
            // Get categories for filter buttons
            ViewBag.Categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
            ViewBag.SelectedCategoryId = searchModel.CategoryId;
            ViewData["SearchAction"] = "MyRecipes";
            
            // Store search model for the view
            var recipes = await recipesQuery.ToListAsync();
            
            // Add favorite status for each recipe
            if (User.Identity.IsAuthenticated)
            {
                var favoriteRecipeIds = await _context.UserFavorites
                    .Where(uf => uf.UserId == userId)
                    .Select(uf => uf.RecipeId)
                    .ToListAsync();
                    
                ViewBag.FavoriteRecipes = favoriteRecipeIds;
            }
            return View(recipes);
        }

        // GET: Recipe/MyFavorites
        [Authorize]
        public async Task<IActionResult> MyFavorites(RecipeSearchViewModel searchModel)
        {
            // Save search parameters to ViewData for form repopulation
            ViewData["SearchTerm"] = searchModel.SearchTerm;
            ViewData["SearchType"] = searchModel.SearchType ?? "all";
            ViewData["Difficulty"] = searchModel.Difficulty;
            ViewData["SortBy"] = searchModel.SortBy ?? "newest";
            ViewData["MatchAllIngredients"] = searchModel.MatchAllIngredients.ToString().ToLower();
            ViewData["CategoryId"] = searchModel.CategoryId;
            
            var userId = _userManager.GetUserId(User);
            
            // Get user's favorite recipes
            var favoriteRecipesQuery = _context.UserFavorites
                .Where(uf => uf.UserId == userId)
                .Include(uf => uf.Recipe)
                    .ThenInclude(r => r.CreatedBy)
                .Include(uf => uf.Recipe)
                    .ThenInclude(r => r.RecipeCategories)
                        .ThenInclude(rc => rc.Category)
                .Select(uf => uf.Recipe)
                .AsQueryable();
            
            // Apply search filters based search model
            favoriteRecipesQuery = ApplySearchFilters(favoriteRecipesQuery, searchModel);
            
            // Get categories for filter buttons
            ViewBag.Categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
            ViewBag.SelectedCategoryId = searchModel.CategoryId;
            ViewData["SearchAction"] = "MyFavorites";
            
            // Store search model
            var recipes = await favoriteRecipesQuery.ToListAsync();
            
            // Pass the favorite IDs
            if (User.Identity.IsAuthenticated)
            {
                var favoriteRecipeIds = await _context.UserFavorites
                    .Where(uf => uf.UserId == userId)
                    .Select(uf => uf.RecipeId)
                    .ToListAsync();
            
                ViewBag.FavoriteRecipes = favoriteRecipeIds;
            }
            
            return View(recipes);
        }

        // POST: Recipe/ToggleFavorite
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleFavorite(int recipeId)
        {
            try
            {
                var userId = _userManager.GetUserId(User);
        
                // Check if recipe exists
                var recipe = await _context.Recipes.FindAsync(recipeId);
                if (recipe == null)
                {
                    return Json(new { success = false, message = "Recipe not found" });
                }
        
                // Check if already favorited
                var existingFavorite = await _context.UserFavorites
                    .FirstOrDefaultAsync(uf => uf.UserId == userId && uf.RecipeId == recipeId);
        
                if (existingFavorite != null)
                {
                    // Remove favorite
                    _context.UserFavorites.Remove(existingFavorite);
                    await _context.SaveChangesAsync();
                    return Json(new { success = true, isFavorite = false });
                }
                else
                {
                    // Add favorite
                    var favorite = new UserFavoriteEntity
                    {
                        UserId = userId,
                        RecipeId = recipeId,
                        DateAdded = DateTime.UtcNow
                    };
            
                    _context.UserFavorites.Add(favorite);
                    await _context.SaveChangesAsync();
                    return Json(new { success = true, isFavorite = true });
                }
            }
            catch (Exception ex)
            {
                // Log the error if you have logging
                return Json(new { success = false, message = "An error occurred: " + ex.Message });
            }
        }

        // Helper method to check if a recipe is favorited by current user
        private async Task<bool> IsRecipeFavorited(int recipeId)
        {
            if (!User.Identity.IsAuthenticated)
                return false;
                
            var userId = _userManager.GetUserId(User);
            return await _context.UserFavorites
                .AnyAsync(uf => uf.UserId == userId && uf.RecipeId == recipeId);
        }

        // Helper method to apply search filters to a recipe query
        private IQueryable<RecipeEntity> ApplySearchFilters(IQueryable<RecipeEntity> query, RecipeSearchViewModel searchModel)
        {
            // Apply search term filtering
            if (!string.IsNullOrEmpty(searchModel.SearchTerm))
            {
                // Split search term into individual terms
                var searchTerms = searchModel.SearchTerm.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(t => t.Trim().ToLower()) // Convert to lowercase for case-insensitive search
                    .Where(t => !string.IsNullOrEmpty(t))
                    .ToList();
                
                if (searchTerms.Any())
                {
                    switch (searchModel.SearchType)
                    {
                        case "title":
                            // Search only in titles
                            foreach (var term in searchTerms)
                            {
                                query = query.Where(r => r.Title.ToLower().Contains(term));
                            }
                            break;
                            
                        case "ingredients":
                            // Search only in ingredients
                            if (searchModel.MatchAllIngredients)
                            {
                                // AND logic - must contain all terms
                                foreach (var term in searchTerms)
                                {
                                    query = query.Where(r => r.Ingredients.ToLower().Contains(term));
                                }
                            }
                            else
                            {
                                // OR logic - must contain any of the terms
                                query = query.Where(r => 
                                    searchTerms.Any(term => r.Ingredients.ToLower().Contains(term)));
                            }
                            break;
                            
                        case "all":
                        default:
                            // Search in title, description and ingredients
                            if (searchTerms.Count == 1)
                            {
                                // Single term search
                                var term = searchTerms[0];
                                query = query.Where(r => 
                                    r.Title.ToLower().Contains(term) || 
                                    r.Description.ToLower().Contains(term) || 
                                    r.Ingredients.ToLower().Contains(term));
                            }
                            else
                            {
                                // Multi-term search with comma or space separated terms
                                if (searchModel.MatchAllIngredients)
                                {
                                    // AND logic for ingredients, OR logic for title/description
                                    var ingredientMatches = query.Where(r => true);
                                    foreach (var term in searchTerms)
                                    {
                                        ingredientMatches = ingredientMatches.Where(r => r.Ingredients.ToLower().Contains(term));
                                    }
                                    
                                    var titleDescMatches = query.Where(r => 
                                        searchTerms.Any(term => 
                                            r.Title.ToLower().Contains(term) || r.Description.ToLower().Contains(term)));
                                    
                                    var ingredientIds = ingredientMatches.Select(r => r.Id).ToList();
                                    var titleDescIds = titleDescMatches.Select(r => r.Id).ToList();
                                    var allIds = ingredientIds.Union(titleDescIds).ToList();
                                    
                                    query = query.Where(r => allIds.Contains(r.Id));
                                }
                                else
                                {
                                    // OR logic for everything
                                    query = query.Where(r => 
                                        searchTerms.Any(term => 
                                            r.Title.ToLower().Contains(term) || 
                                            r.Description.ToLower().Contains(term) || 
                                            r.Ingredients.ToLower().Contains(term)));
                                }
                            }
                            break;
                    }
                }
            }
            
            // Apply ingredients filter if specified
            if (searchModel.Ingredients != null && searchModel.Ingredients.Any())
            {
                if (searchModel.MatchAllIngredients)
                {
                    // AND logic - must contain all ingredients
                    foreach (var ingredient in searchModel.Ingredients)
                    {
                        query = query.Where(r => r.Ingredients.ToLower().Contains(ingredient.ToLower()));
                    }
                }
                else
                {
                    // OR logic - must contain any of the ingredients
                    var ingredients = searchModel.Ingredients.Select(i => i.ToLower()).ToList();
                    query = query.Where(r => 
                        ingredients.Any(ingredient => r.Ingredients.ToLower().Contains(ingredient)));
                }
            }
            
            // Apply difficulty filter
            if (!string.IsNullOrEmpty(searchModel.Difficulty))
            {
                if (Enum.TryParse<DifficultyLevel>(searchModel.Difficulty, out var difficultyLevel))
                {
                    query = query.Where(r => r.Difficulty == difficultyLevel);
                }
            }
            
            // Apply servings range filter
            if (searchModel.MinServings.HasValue)
            {
                query = query.Where(r => r.Servings >= searchModel.MinServings.Value);
            }
            
            if (searchModel.MaxServings.HasValue)
            {
                query = query.Where(r => r.Servings <= searchModel.MaxServings.Value);
            }
            
            // Apply preparation time range filter
            if (searchModel.MinPrepTime.HasValue)
            {
                query = query.Where(r => (r.PrepTimeMinutes + r.CookTimeMinutes) >= searchModel.MinPrepTime.Value);
            }
            
            if (searchModel.MaxPrepTime.HasValue)
            {
                query = query.Where(r => (r.PrepTimeMinutes + r.CookTimeMinutes) <= searchModel.MaxPrepTime.Value);
            }
            
            // Apply category filter
            if (searchModel.CategoryId.HasValue)
            {
                query = query.Where(r => r.RecipeCategories.Any(rc => rc.CategoryId == searchModel.CategoryId.Value));
            }
            
            // Apply sorting
            switch (searchModel.SortBy)
            {
                case "oldest":
                    query = query.OrderBy(r => r.CreatedDate);
                    break;
                case "fastest":
                    query = query.OrderBy(r => r.PrepTimeMinutes + r.CookTimeMinutes);
                    break;
                case "easiest":
                    query = query.OrderBy(r => r.Difficulty);
                    break;
                case "newest":
                default:
                    query = query.OrderByDescending(r => r.CreatedDate);
                    break;
            }
            
            return query;
        }

        // API endpoint for recipe name autocomplete
        [HttpGet]
        public async Task<IActionResult> GetRecipeSuggestions(string term)
        {
            if (string.IsNullOrEmpty(term))
            {
                return Json(new List<string>());
            }
            
            var lowerTerm = term.ToLower();
            var suggestions = await _context.Recipes
                .Where(r => r.Title.ToLower().Contains(lowerTerm))
                .Select(r => r.Title)
                .Distinct()
                .Take(10)
                .ToListAsync();
                
            return Json(suggestions);
        }
        
        // API endpoint for search suggestions (both recipes and ingredients)
        [HttpGet]
        public async Task<IActionResult> GetSearchSuggestions(string term)
        {
            if (string.IsNullOrEmpty(term))
            {
                return Json(new List<string>());
            }
            
            var lowerTerm = term.ToLower();
            
            // Get recipe title suggestions
            var recipeSuggestions = await _context.Recipes
                .Where(r => r.Title.ToLower().Contains(lowerTerm))
                .Select(r => r.Title)
                .Distinct()
                .Take(5)
                .ToListAsync();
            
            // Get ingredient suggestions
            var ingredientSuggestions = await _context.Ingredients
                .Where(i => i.Name.ToLower().Contains(lowerTerm))
                .Select(i => i.Name)
                .Distinct()
                .Take(5)
                .ToListAsync();
            
            // Combine and return suggestions
            var allSuggestions = recipeSuggestions
                .Union(ingredientSuggestions)
                .OrderBy(s => s)
                .Take(10)
                .ToList();
            
            return Json(allSuggestions);
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

            // Check if the recipe is favorited by the current user
            if (User.Identity.IsAuthenticated)
            {
                ViewBag.IsFavorited = await IsRecipeFavorited(id);
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