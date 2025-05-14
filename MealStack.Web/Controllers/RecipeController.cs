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
using System.Diagnostics;

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
        public async Task<IActionResult> Index(RecipeSearchViewModel searchModel, int page = 1)
        {
            return await TryExecuteAsync(async () =>
            {
                int pageSize = 12;
                
                ViewData["SearchTerm"] = searchModel.SearchTerm;
                ViewData["SearchType"] = searchModel.SearchType ?? "all";
                ViewData["Difficulty"] = searchModel.Difficulty;
                ViewData["SortBy"] = searchModel.SortBy ?? "newest";
                ViewData["MatchAllIngredients"] = searchModel.MatchAllIngredients.ToString().ToLower();
                ViewData["CategoryId"] = searchModel.CategoryId;
                
                var recipesQuery = _context.Recipes
                    .Include(r => r.CreatedBy)
                    .Include(r => r.RecipeCategories)
                    .ThenInclude(rc => rc.Category)
                    .Include(r => r.Ratings)
                    .AsQueryable();
                
                recipesQuery = ApplySearchFilters(recipesQuery, searchModel);
                
                var totalRecipes = await recipesQuery.CountAsync();
                
                var recipes = await recipesQuery
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();
                
                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = (int)Math.Ceiling(totalRecipes / (double)pageSize);
                
                
                ViewBag.Categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
                ViewBag.SelectedCategoryId = searchModel.CategoryId;
                ViewData["SearchAction"] = "Index";
                
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
            }, "Error loading recipes. Please try again later.");
        }

        // GET: Recipe/MyRecipes
        [Authorize]
        public async Task<IActionResult> MyRecipes(RecipeSearchViewModel searchModel, int page = 1)
        {
            return await TryExecuteAsync(async () =>
            {
                int pageSize = 12;
                
                ViewData["SearchTerm"] = searchModel.SearchTerm;
                ViewData["SearchType"] = searchModel.SearchType ?? "all";
                ViewData["Difficulty"] = searchModel.Difficulty;
                ViewData["SortBy"] = searchModel.SortBy ?? "newest";
                ViewData["MatchAllIngredients"] = searchModel.MatchAllIngredients.ToString().ToLower();
                ViewData["CategoryId"] = searchModel.CategoryId;
                
                var userId = _userManager.GetUserId(User);
                
                var recipesQuery = _context.Recipes
                    .Include(r => r.CreatedBy)
                    .Include(r => r.RecipeCategories)
                    .ThenInclude(rc => rc.Category)
                    .Include(r => r.Ratings) // Include ratings for sorting and display
                    .Where(r => r.CreatedById == userId)
                    .AsQueryable();
                
                recipesQuery = ApplySearchFilters(recipesQuery, searchModel);
                
                var totalRecipes = await recipesQuery.CountAsync();
                
                var recipes = await recipesQuery
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();
                
                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = (int)Math.Ceiling(totalRecipes / (double)pageSize);
                
                ViewBag.Categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
                ViewBag.SelectedCategoryId = searchModel.CategoryId;
                ViewData["SearchAction"] = "MyRecipes";
                
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
            }, "Error loading your recipes. Please try again later.");
        }

        // GET: Recipe/MyFavorites
        [Authorize]
        public async Task<IActionResult> MyFavorites(RecipeSearchViewModel searchModel, int page = 1)
        {
            return await TryExecuteAsync(async () =>
            {
                int pageSize = 12;
                
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
                    .Include(uf => uf.Recipe.Ratings)
                    .Select(uf => uf.Recipe)
                    .AsQueryable();
                
                favoriteRecipesQuery = ApplySearchFilters(favoriteRecipesQuery, searchModel);
                
                var totalRecipes = await favoriteRecipesQuery.CountAsync();
                
                var recipes = await favoriteRecipesQuery
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();
                
                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = (int)Math.Ceiling(totalRecipes / (double)pageSize);
                
                ViewBag.Categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
                ViewBag.SelectedCategoryId = searchModel.CategoryId;
                ViewData["SearchAction"] = "MyFavorites";
                
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
            }, "Error loading your favorite recipes. Please try again later.");
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
        
                var recipe = await _context.Recipes.FindAsync(recipeId);
                if (recipe == null)
                {
                    return Json(new { success = false, message = "Recipe not found" });
                }
        
                var existingFavorite = await _context.UserFavorites
                    .FirstOrDefaultAsync(uf => uf.UserId == userId && uf.RecipeId == recipeId);
        
                if (existingFavorite != null)
                {
                    _context.UserFavorites.Remove(existingFavorite);
                    await _context.SaveChangesAsync();
                    return Json(new { success = true, isFavorite = false });
                }
                else
                {
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
                Debug.WriteLine($"Error in ToggleFavorite: {ex.Message}");
                return Json(new { success = false, message = "An error occurred: " + ex.Message });
            }
        }

        // POST: Recipe/RateRecipe
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RateRecipe(int recipeId, int rating)
        {
            try
            {
                if (rating < 1 || rating > 5)
                    return Json(new { success = false, message = "Invalid rating" });

                var userId = _userManager.GetUserId(User);
                
                
                var recipe = await _context.Recipes.FindAsync(recipeId);
                if (recipe == null)
                {
                    return Json(new { success = false, message = "Recipe not found" });
                }
                
                var existingRating = await _context.UserRatings
                    .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RecipeId == recipeId);
                
                if (existingRating != null)
                {
                    existingRating.Rating = rating;
                    existingRating.DateRated = DateTime.UtcNow;
                }
                else
                {
                    _context.UserRatings.Add(new UserRatingEntity
                    {
                        UserId = userId,
                        RecipeId = recipeId,
                        Rating = rating,
                        DateRated = DateTime.UtcNow
                    });
                }
                
                await _context.SaveChangesAsync();
                
                // Get updated average
                var averageRating = await _context.UserRatings
                    .Where(ur => ur.RecipeId == recipeId)
                    .AverageAsync(ur => (double)ur.Rating);
                
                var totalRatings = await _context.UserRatings
                    .Where(ur => ur.RecipeId == recipeId)
                    .CountAsync();
                
                return Json(new { 
                    success = true, 
                    averageRating = Math.Round(averageRating, 1),
                    totalRatings = totalRatings
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in RateRecipe: {ex.Message}");
                return Json(new { success = false, message = "An error occurred while rating recipe: " + ex.Message });
            }
        }

        // GET: Recipe/SaveNotes
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveNotes(int recipeId, string notes)
        {
            try
            {
                var recipe = await _context.Recipes.FindAsync(recipeId);
                if (recipe == null)
                {
                    return Json(new { success = false, message = "Recipe not found" });
                }

                var userId = _userManager.GetUserId(User);
                if (recipe.CreatedById != userId && !User.IsInRole("Admin"))
                {
                    return Json(new { success = false, message = "Not authorized to add notes to this recipe" });
                }

                recipe.Notes = notes;
                recipe.UpdatedDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in SaveNotes: {ex.Message}");
                return Json(new { success = false, message = "An error occurred: " + ex.Message });
            }
        }

        // Check if a recipe is favorited by current user
        private async Task<bool> IsRecipeFavorited(int recipeId)
        {
            if (!User.Identity.IsAuthenticated)
                return false;
                
            var userId = _userManager.GetUserId(User);
            return await _context.UserFavorites
                .AnyAsync(uf => uf.UserId == userId && uf.RecipeId == recipeId);
        }

        // Check if a recipe is rated by current user
        private async Task<int?> GetUserRating(int recipeId)
        {
            if (!User.Identity.IsAuthenticated)
                return null;
                
            var userId = _userManager.GetUserId(User);
            var rating = await _context.UserRatings
                .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RecipeId == recipeId);
            
            return rating?.Rating;
        }

        // GET: Recipe/Details/5
        public async Task<IActionResult> Details(int id)
        {
            return await TryExecuteAsync(async () =>
            {
                var recipe = await _context.Recipes
                    .Include(r => r.CreatedBy)
                    .Include(r => r.RecipeCategories)
                    .ThenInclude(rc => rc.Category)
                    .Include(r => r.Ratings) // Include ratings
                    .FirstOrDefaultAsync(r => r.Id == id);
                    
                if (recipe == null)
                {
                    return NotFound();
                }

                if (User.Identity.IsAuthenticated)
                {
                    ViewBag.IsFavorited = await IsRecipeFavorited(id);
                    ViewBag.UserRating = await GetUserRating(id);
                }

                return View(recipe);
            }, "Error loading recipe details. Please try again later.");
        }

        // GET: Recipe/Create
        [Authorize]
        public async Task<IActionResult> Create()
        {
            return await TryExecuteAsync(async () =>
            {
                var recipe = new RecipeEntity
                {
                    Difficulty = DifficultyLevel.Easy,
                    PrepTimeMinutes = 15,
                    CookTimeMinutes = 30,
                    Servings = 4
                };
                
                ViewBag.Categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
                
                return View(recipe);
            }, "Error loading recipe form. Please try again later.");
        }

        // POST: Recipe/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Create(RecipeEntity recipe, int[]? selectedCategories)
        {
            return await TryExecuteAsync(async () =>
            {
                if (recipe == null)
                {
                    ModelState.AddModelError("", "Invalid recipe data");
                    ViewBag.Categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
                    return View(new RecipeEntity());
                }

                recipe.CreatedById = _userManager.GetUserId(User) ?? string.Empty;
                recipe.CreatedDate = DateTime.UtcNow;
                
                ModelState.Remove("CreatedById");
                
                // Ensure Not null to avoid DB constraints
                recipe.Ingredients = recipe.Ingredients ?? string.Empty;
                recipe.Description = recipe.Description ?? string.Empty;
                
                if (ModelState.IsValid)
                {
                    // No duplicate titles per user
                    bool duplicateExists = await _context.Recipes
                        .AnyAsync(r => r.Title.ToLower() == recipe.Title.ToLower() && 
                                       r.CreatedById == recipe.CreatedById);
                        
                    if (duplicateExists)
                    {
                        ModelState.AddModelError("Title", "You already have a recipe with this title. Please use a different title.");
                        ViewBag.Categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
                        return View(recipe);
                    }
                    
                    // Add and save the recipe to get an ID
                    _context.Recipes.Add(recipe);
                    await _context.SaveChangesAsync();
                    
                    // Then add categories if any were selected
                    if (selectedCategories != null && selectedCategories.Length > 0)
                    {
                        foreach (var categoryId in selectedCategories)
                        {
                            _context.RecipeCategories.Add(new RecipeCategoryEntity
                            {
                                RecipeId = recipe.Id,
                                CategoryId = categoryId
                            });
                        }
                        await _context.SaveChangesAsync();
                    }
                    
                    TempData["Message"] = "Recipe created successfully!";
                    return RedirectToAction("Details", new { id = recipe.Id });
                }
                
                // If we got this far, something failed, redisplay form
                ViewBag.Categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
                return View(recipe);
            }, "Error creating recipe. Please check your input and try again.");
        }

        // GET: Recipe/Edit/5
        [Authorize]
        public async Task<IActionResult> Edit(int id)
        {
            return await TryExecuteAsync(async () =>
            {
                var recipe = await _context.Recipes
                    .Include(r => r.RecipeCategories)
                    .FirstOrDefaultAsync(r => r.Id == id);
                    
                if (recipe == null)
                {
                    return NotFound();
                }

                
                if (recipe.CreatedById == _userManager.GetUserId(User) || User.IsInRole("Admin"))
                {
                    // Get categories for dropdown and mark the selected ones
                    ViewBag.Categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
                    ViewBag.SelectedCategories = recipe.RecipeCategories?.Select(rc => rc.CategoryId).ToList() ?? new List<int>();
                    
                    return View(recipe);
                }
                
                return Forbid();
            }, "Error loading recipe for editing. Please try again later.");
        }

        // POST: Recipe/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Edit(int id, RecipeEntity recipe, int[] selectedCategories)
        {
            return await TryExecuteAsync(async () =>
            {
                if (id != recipe.Id)
                {
                    return NotFound();
                }
                
                var existingRecipe = await _context.Recipes
                    .AsNoTracking()
                    .FirstOrDefaultAsync(r => r.Id == id);
                    
                if (existingRecipe == null)
                {
                    return NotFound();
                }
                
                var userId = _userManager.GetUserId(User);
                if (existingRecipe.CreatedById != userId && !User.IsInRole("Admin"))
                {
                    return Forbid();
                }
                
                recipe.CreatedById = existingRecipe.CreatedById;
                recipe.CreatedDate = existingRecipe.CreatedDate;
                recipe.UpdatedDate = DateTime.UtcNow;
                
                // Skip validation for
                ModelState.Remove("CreatedById");
                ModelState.Remove("CreatedDate");
                
                recipe.Ingredients = recipe.Ingredients ?? string.Empty;
                recipe.Description = recipe.Description ?? string.Empty;
                
                if (ModelState.IsValid)
                {
                    // Avoid duplicated Recipe name
                    bool duplicateExists = await _context.Recipes
                        .Where(r => r.Id != id && r.CreatedById == recipe.CreatedById)
                        .AnyAsync(r => r.Title.ToLower() == recipe.Title.ToLower());
                        
                    if (duplicateExists)
                    {
                        ModelState.AddModelError("Title", "You already have another recipe with this title. Please use a different title.");
                        ViewBag.Categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
                        ViewBag.SelectedCategories = await _context.RecipeCategories
                            .Where(rc => rc.RecipeId == id)
                            .Select(rc => rc.CategoryId)
                            .ToListAsync();
                        return View(recipe);
                    }
                
                    using var transaction = await _context.Database.BeginTransactionAsync();
                    
                    try
                    {
                        // Use EntityState.Modified instead of tracking and property updates
                        _context.Entry(recipe).State = EntityState.Modified;
                        
                        // Save the basic changes first
                        await _context.SaveChangesAsync();
                        
                        // Handle categories - remove existing and add new ones
                        var existingCategories = await _context.RecipeCategories
                            .Where(rc => rc.RecipeId == id)
                            .ToListAsync();
                            
                        _context.RecipeCategories.RemoveRange(existingCategories);
                        await _context.SaveChangesAsync();
                        
                        // Add new categories if any were selected
                        if (selectedCategories != null && selectedCategories.Length > 0)
                        {
                            foreach (var categoryId in selectedCategories)
                            {
                                _context.RecipeCategories.Add(new RecipeCategoryEntity
                                {
                                    RecipeId = id,
                                    CategoryId = categoryId
                                });
                            }
                            
                            await _context.SaveChangesAsync();
                        }
                        
                        await transaction.CommitAsync();
                        TempData["Message"] = "Recipe updated successfully!";
                        return RedirectToAction("Details", new { id = id });
                    }
                    catch
                    {
                        await transaction.RollbackAsync();
                        throw; 
                    }
                }
                
                // If we got this far, something failed - redisplay form
                ViewBag.Categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
                ViewBag.SelectedCategories = await _context.RecipeCategories
                    .Where(rc => rc.RecipeId == id)
                    .Select(rc => rc.CategoryId)
                    .ToListAsync();
                    
                return View(recipe);
            }, "Error updating recipe. Please check your input and try again.");
        }

        // POST: Recipe/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Delete(int id)
        {
            return await TryExecuteAsync(async () =>
            {
                var recipe = await _context.Recipes.FindAsync(id);
                if (recipe == null)
                {
                    return NotFound();
                }

                
                if (recipe.CreatedById == _userManager.GetUserId(User) || User.IsInRole("Admin"))
                {
                    _context.Recipes.Remove(recipe);
                    await _context.SaveChangesAsync();
                    
                    TempData["Message"] = "Recipe deleted successfully!";
                    return RedirectToAction("MyRecipes");
                }
                
                return Forbid();
            }, "Error deleting recipe. Please try again later.", "MyRecipes");
        }

        // API endpoint for recipe name autocomplete
        [HttpGet]
        public async Task<IActionResult> GetRecipeSuggestions(string term)
        {
            try
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
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetRecipeSuggestions: {ex.Message}");
                return Json(new List<string>());
            }
        }
        
        // API endpoint for search suggestions (both recipes and ingredients)
        [HttpGet]
        public async Task<IActionResult> GetSearchSuggestions(string term)
        {
            try
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
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetSearchSuggestions: {ex.Message}");
                return Json(new List<string>());
            }
        }
        
        private IQueryable<RecipeEntity> ApplySearchFilters(IQueryable<RecipeEntity> query, RecipeSearchViewModel searchModel)
        {
            // Apply search term filtering
            if (!string.IsNullOrEmpty(searchModel.SearchTerm))
            {
                // Split search term in individual terms
                var searchTerms = searchModel.SearchTerm.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(t => t.Trim().ToLower())
                    .Where(t => !string.IsNullOrEmpty(t))
                    .ToList();
                
                if (searchTerms.Any())
                {
                    ApplySearchTermFilters(ref query, searchModel.SearchType, searchTerms, searchModel.MatchAllIngredients);
                }
            }
            
            // Apply other filters (separate method - clarity)
            query = ApplyAdditionalFilters(query, searchModel);
            
            // Apply sorting
            query = ApplySorting(query, searchModel.SortBy);
            
            return query;
        }

        // Extract search term filtering logic to a separate method
        private void ApplySearchTermFilters(ref IQueryable<RecipeEntity> query, string searchType, List<string> searchTerms, bool matchAllIngredients)
        {
            switch (searchType)
            {
                case "title":
                    // Search only in titles
                    foreach (var term in searchTerms)
                    {
                        query = query.Where(r => r.Title.ToLower().Contains(term));
                    }
                    break;
                    
                case "ingredients":
                    if (matchAllIngredients)
                    {
                        // AND logic - Need to match all terms
                        foreach (var term in searchTerms)
                        {
                            query = query.Where(r => r.Ingredients.ToLower().Contains(term));
                        }
                    }
                    else
                    {
                        // OR logic - Match any term
                        query = query.Where(r => 
                            searchTerms.Any(term => r.Ingredients.ToLower().Contains(term)));
                    }
                    break;
                    
                case "all":
                default:
                    ApplyAllFieldsSearch(ref query, searchTerms, matchAllIngredients);
                    break;
            }
        }

        // Extract all-fields search logic
        private void ApplyAllFieldsSearch(ref IQueryable<RecipeEntity> query, List<string> searchTerms, bool matchAllIngredients)
        {
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
                if (matchAllIngredients)
                {
                    // Match ALL for ingredients, ANY for title/desc
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
                    // Or logic - Match anything
                    query = query.Where(r => 
                        searchTerms.Any(term => 
                            r.Title.ToLower().Contains(term) || 
                            r.Description.ToLower().Contains(term) || 
                            r.Ingredients.ToLower().Contains(term)));
                }
            }
        }

        // Apply additional filters (difficulty, servings, prep time, categories)
        private IQueryable<RecipeEntity> ApplyAdditionalFilters(IQueryable<RecipeEntity> query, RecipeSearchViewModel searchModel)
        {
            // Ingredients
            if (searchModel.Ingredients != null && searchModel.Ingredients.Any())
            {
                ApplyIngredientsFilter(ref query, searchModel.Ingredients, searchModel.MatchAllIngredients);
            }
            
            // Difficulty
            if (!string.IsNullOrEmpty(searchModel.Difficulty))
            {
                if (Enum.TryParse<DifficultyLevel>(searchModel.Difficulty, out var difficultyLevel))
                {
                    query = query.Where(r => r.Difficulty == difficultyLevel);
                }
            }
            
            // Servings range
            if (searchModel.MinServings.HasValue)
            {
                query = query.Where(r => r.Servings >= searchModel.MinServings.Value);
            }
            
            if (searchModel.MaxServings.HasValue)
            {
                query = query.Where(r => r.Servings <= searchModel.MaxServings.Value);
            }
            
            // Preparation time range
            if (searchModel.MinPrepTime.HasValue)
            {
                query = query.Where(r => (r.PrepTimeMinutes + r.CookTimeMinutes) >= searchModel.MinPrepTime.Value);
            }
            
            if (searchModel.MaxPrepTime.HasValue)
            {
                query = query.Where(r => (r.PrepTimeMinutes + r.CookTimeMinutes) <= searchModel.MaxPrepTime.Value);
            }
            
            // Category 
            if (searchModel.CategoryId.HasValue)
            {
                query = query.Where(r => r.RecipeCategories.Any(rc => rc.CategoryId == searchModel.CategoryId.Value));
            }
            
            return query;
        }

        private IQueryable<RecipeEntity> ApplySorting(IQueryable<RecipeEntity> query, string sortBy)
        {
            switch (sortBy)
            {
                case "oldest":
                    return query.OrderBy(r => r.CreatedDate);
                case "fastest":
                    return query.OrderBy(r => r.PrepTimeMinutes + r.CookTimeMinutes);
                case "easiest":
                    return query.OrderBy(r => r.Difficulty);
                case "highestRated":
                    return query.OrderByDescending(r => r.Ratings.Any() ? r.Ratings.Average(ur => ur.Rating) : 0);
                case "newest":
                default:
                    return query.OrderByDescending(r => r.CreatedDate);
            }
        }

        private void ApplyIngredientsFilter(ref IQueryable<RecipeEntity> query, List<string> ingredients, bool matchAllIngredients)
        {
            if (matchAllIngredients)
            {
                // AND logic - must contain all ingredients
                foreach (var ingredient in ingredients)
                {
                    query = query.Where(r => r.Ingredients.ToLower().Contains(ingredient.ToLower()));
                }
            }
            else
            {
                // OR logic - must contain any of the ingredients
                var ingredientTerms = ingredients.Select(i => i.ToLower()).ToList();
                query = query.Where(r => 
                    ingredientTerms.Any(ingredient => r.Ingredients.ToLower().Contains(ingredient)));
            }
        }

        private bool RecipeExists(int id)
        {
            return _context.Recipes.Any(e => e.Id == id);
        }
    }
}