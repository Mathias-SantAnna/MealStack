using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MealStack.Infrastructure.Data;
using MealStack.Infrastructure.Data.Entities;
using MealStack.Web.Models;
using System.Diagnostics;

namespace MealStack.Web.Controllers
{
    public class RecipeController : BaseController
    {
        private readonly MealStackDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<RecipeController> _logger;

        public RecipeController(
            MealStackDbContext context, 
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment webHostEnvironment,
            ILogger<RecipeController> logger) 
            : base(userManager)
        {
            _context = context;
            _userManager = userManager;
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
        }
        
        public async Task<IActionResult> Index(RecipeSearchViewModel searchModel, int page = 1)
        {
            return await TryExecuteAsync(async () =>
            {
                int pageSize = 12;
                
                SetupViewDataForSearch(searchModel);
                
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
                
                await SetupPaginationAndCategories(page, totalRecipes, pageSize, searchModel.CategoryId);
                ViewData["SearchAction"] = "Index";
                
                await AddFavoriteStatusToViewBag();
                
                return View(recipes);
            }, "Error loading recipes. Please try again later.");
        }

        [Authorize]
        public async Task<IActionResult> MyRecipes(RecipeSearchViewModel searchModel, int page = 1)
        {
            return await TryExecuteAsync(async () =>
            {
                int pageSize = 12;
                
                SetupViewDataForSearch(searchModel);
                
                var userId = _userManager.GetUserId(User);
                
                var recipesQuery = _context.Recipes
                    .Include(r => r.CreatedBy)
                    .Include(r => r.RecipeCategories)
                    .ThenInclude(rc => rc.Category)
                    .Include(r => r.Ratings) 
                    .Where(r => r.CreatedById == userId)
                    .AsQueryable();
                
                recipesQuery = ApplySearchFilters(recipesQuery, searchModel);
                
                var totalRecipes = await recipesQuery.CountAsync();
                
                var recipes = await recipesQuery
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();
                
                await SetupPaginationAndCategories(page, totalRecipes, pageSize, searchModel.CategoryId);
                ViewData["SearchAction"] = "MyRecipes";
                
                await AddFavoriteStatusToViewBag();
                return View(recipes);
            }, "Error loading your recipes. Please try again later.");
        }

        [Authorize]
        public async Task<IActionResult> MyFavorites(RecipeSearchViewModel searchModel, int page = 1)
        {
            return await TryExecuteAsync(async () =>
            {
                int pageSize = 12;
                
                SetupViewDataForSearch(searchModel);
                
                var userId = _userManager.GetUserId(User);
                
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
                
                await SetupPaginationAndCategories(page, totalRecipes, pageSize, searchModel.CategoryId);
                ViewData["SearchAction"] = "MyFavorites";
                
                await AddFavoriteStatusToViewBag();
                
                return View(recipes);
            }, "Error loading your favorite recipes. Please try again later.");
        }

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
                _logger.LogError(ex, "Error in ToggleFavorite for recipe {RecipeId}", recipeId);
                return Json(new { success = false, message = "An error occurred: " + ex.Message });
            }
        }

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
                _logger.LogError(ex, "Error rating recipe {RecipeId}", recipeId);
                return Json(new { success = false, message = "An error occurred while rating recipe: " + ex.Message });
            }
        }

        public async Task<IActionResult> Details(int id)
        {
            var recipe = await _context.Recipes
                .Include(r => r.CreatedBy)
                .Include(r => r.RecipeCategories)
                .ThenInclude(rc => rc.Category)
                .Include(r => r.Ratings)
                .FirstOrDefaultAsync(r => r.Id == id);
        
            if (recipe == null)
            {
                return NotFound();
            }

            await SetUserSpecificDataForRecipe(recipe);

            return View(recipe);
        }

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

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Create(RecipeEntity recipe, IFormFile ImageFile, int[]? selectedCategories)
        {
            return await TryExecuteAsync(async () =>
            {
                _logger.LogInformation("Starting recipe creation for user {UserId}", _userManager.GetUserId(User));

                if (recipe == null)
                {
                    _logger.LogWarning("Recipe object is null in Create action");
                    ModelState.AddModelError("", "Invalid recipe data");
                    ViewBag.Categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
                    return View(new RecipeEntity());
                }

                recipe.CreatedById = _userManager.GetUserId(User) ?? string.Empty;
                recipe.CreatedDate = DateTime.UtcNow;
                
                ModelState.Remove("CreatedById");
                ModelState.Remove("CreatedDate");
                ModelState.Remove("CreatedBy");
                
                recipe.Ingredients = recipe.Ingredients ?? string.Empty;
                recipe.Description = recipe.Description ?? string.Empty;
                recipe.Notes = recipe.Notes ?? string.Empty;

                _logger.LogInformation("Recipe ingredients data: {Ingredients}", recipe.Ingredients);
                
                if (ImageFile != null && ImageFile.Length > 0)
                {
                    try
                    {
                        recipe.ImagePath = await SaveRecipeImage(ImageFile);
                        _logger.LogInformation("Image saved to: {ImagePath}", recipe.ImagePath);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error saving recipe image");
                        ModelState.AddModelError("ImageFile", "Error uploading image. Please try again.");
                    }
                }
                
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("ModelState is invalid for recipe creation");
                    foreach (var modelError in ModelState)
                    {
                        foreach (var error in modelError.Value.Errors)
                        {
                            _logger.LogWarning("ModelState error - {Field}: {Error}", modelError.Key, error.ErrorMessage);
                        }
                    }
                    
                    ViewBag.Categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
                    return View(recipe);
                }

                bool duplicateExists = await _context.Recipes
                    .AnyAsync(r => r.Title.ToLower() == recipe.Title.ToLower() && 
                                   r.CreatedById == recipe.CreatedById);
                    
                if (duplicateExists)
                {
                    _logger.LogWarning("Duplicate recipe title '{Title}' for user {UserId}", recipe.Title, recipe.CreatedById);
                    ModelState.AddModelError("Title", "You already have a recipe with this title. Please use a different title.");
                    ViewBag.Categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
                    return View(recipe);
                }

                using var transaction = await _context.Database.BeginTransactionAsync();
                
                try
                {
                    _context.Recipes.Add(recipe);
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation("Recipe saved with ID: {RecipeId}", recipe.Id);
                    
                    if (selectedCategories != null && selectedCategories.Length > 0)
                    {
                        await AddCategoriesToRecipe(recipe.Id, selectedCategories);
                        _logger.LogInformation("Added {CategoryCount} categories to recipe {RecipeId}", 
                            selectedCategories.Length, recipe.Id);
                    }
                    
                    await transaction.CommitAsync();
                    
                    _logger.LogInformation("Recipe {RecipeId} created successfully by user {UserId}", 
                        recipe.Id, recipe.CreatedById);
                    
                    TempData["Message"] = "Recipe created successfully!";
                    return RedirectToAction("Details", new { id = recipe.Id });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Error saving recipe to database");
                    throw;
                }
            }, "Error creating recipe. Please check your input and try again.");
        }

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

                var userId = _userManager.GetUserId(User);
                if (recipe.CreatedById != userId && !User.IsInRole("Admin"))
                {
                    return Forbid();
                }

                ViewBag.Categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
                ViewBag.SelectedCategories = recipe.RecipeCategories?.Select(rc => rc.CategoryId).ToList() ?? new List<int>();
                
                return View(recipe);
            }, "Error loading recipe for editing. Please try again later.");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Edit(int id, RecipeEntity recipe, IFormFile ImageFile, int[] selectedCategories)
        {
            try
            {
                _logger.LogInformation("Starting Edit for recipe ID: {RecipeId}", id);
                
                if (id != recipe.Id)
                {
                    _logger.LogWarning("ID mismatch: URL ID {UrlId} vs Model ID {ModelId}", id, recipe.Id);
                    return NotFound();
                }
                
                var existingRecipe = await _context.Recipes
                    .Include(r => r.RecipeCategories)
                    .FirstOrDefaultAsync(r => r.Id == id);
                    
                if (existingRecipe == null)
                {
                    _logger.LogWarning("Recipe not found: {RecipeId}", id);
                    return NotFound();
                }
                
                var userId = _userManager.GetUserId(User);
                if (existingRecipe.CreatedById != userId && !User.IsInRole("Admin"))
                {
                    _logger.LogWarning("Unauthorized edit attempt by user {UserId} for recipe {RecipeId}", userId, id);
                    return Forbid();
                }
                
                if (!string.IsNullOrEmpty(recipe.Ingredients))
                {
                    var cleanIngredients = recipe.Ingredients
                        .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                        .Select(line => line.Trim())
                        .Where(line => !string.IsNullOrEmpty(line))
                        .ToList();
                    
                    recipe.Ingredients = string.Join("\n", cleanIngredients);
                    _logger.LogInformation("Cleaned ingredients: {IngredientsCount} lines", cleanIngredients.Count);
                }
                
                if (!string.IsNullOrEmpty(recipe.Instructions))
                {
                    var cleanInstructions = recipe.Instructions
                        .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                        .Select(line => line.Trim())
                        .Where(line => !string.IsNullOrEmpty(line))
                        .ToList();
                    
                    recipe.Instructions = string.Join("\n", cleanInstructions);
                    _logger.LogInformation("Cleaned instructions: {InstructionsCount} lines", cleanInstructions.Count);
                }
                
                ModelState.Remove("CreatedById");
                ModelState.Remove("CreatedDate");
                ModelState.Remove("CreatedBy");
                
                recipe.Ingredients = recipe.Ingredients ?? string.Empty;
                recipe.Description = recipe.Description ?? string.Empty;
                recipe.Notes = recipe.Notes ?? string.Empty;
                
                if (ImageFile == null || ImageFile.Length == 0)
                {
                    ModelState.Remove("ImageFile");
                }
                
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("ModelState invalid for recipe {RecipeId}", id);
                    foreach (var error in ModelState)
                    {
                        foreach (var errorMsg in error.Value.Errors)
                        {
                            _logger.LogWarning("Validation error - {Field}: {Error}", error.Key, errorMsg.ErrorMessage);
                        }
                    }
                    
                    await PrepareViewBagForEdit(id);
                    return View(recipe);
                }
                
                bool duplicateExists = await _context.Recipes
                    .Where(r => r.Id != id && r.CreatedById == existingRecipe.CreatedById)
                    .AnyAsync(r => r.Title.ToLower() == recipe.Title.ToLower());
                    
                if (duplicateExists)
                {
                    ModelState.AddModelError("Title", "You already have another recipe with this title.");
                    await PrepareViewBagForEdit(id);
                    return View(recipe);
                }
                
                if (ImageFile != null && ImageFile.Length > 0)
                {
                    _logger.LogInformation("Processing new image for recipe {RecipeId}", id);
                    
                    if (!string.IsNullOrEmpty(existingRecipe.ImagePath))
                    {
                        DeleteRecipeImage(existingRecipe.ImagePath);
                    }
                    
                    recipe.ImagePath = await SaveRecipeImage(ImageFile);
                }
                else
                {
                    recipe.ImagePath = existingRecipe.ImagePath;
                }
                
                using var transaction = await _context.Database.BeginTransactionAsync();
                
                try
                {
                    existingRecipe.Title = recipe.Title;
                    existingRecipe.Description = recipe.Description;
                    existingRecipe.Ingredients = recipe.Ingredients;
                    existingRecipe.Instructions = recipe.Instructions;
                    existingRecipe.ImagePath = recipe.ImagePath;
                    existingRecipe.PrepTimeMinutes = recipe.PrepTimeMinutes;
                    existingRecipe.CookTimeMinutes = recipe.CookTimeMinutes;
                    existingRecipe.Servings = recipe.Servings;
                    existingRecipe.Difficulty = recipe.Difficulty;
                    existingRecipe.Notes = recipe.Notes;
                    
                    if (string.IsNullOrEmpty(existingRecipe.CreatedById)) {
                        throw new Exception("existingRecipe.CreatedById was lost during update!");
                    }
                    existingRecipe.UpdatedDate = DateTime.UtcNow;
                    
                    _logger.LogInformation("Updated recipe properties for {RecipeId}", id);
                    
                    var existingCategories = existingRecipe.RecipeCategories.ToList();
                    foreach (var category in existingCategories)
                    {
                        _context.RecipeCategories.Remove(category);
                    }
                    
                    _logger.LogInformation("Removed {CategoryCount} existing categories", existingCategories.Count);
                    
                    if (selectedCategories != null && selectedCategories.Length > 0)
                    {
                        foreach (var categoryId in selectedCategories)
                        {
                            var newRecipeCategory = new RecipeCategoryEntity
                            {
                                RecipeId = id,
                                CategoryId = categoryId
                            };
                            _context.RecipeCategories.Add(newRecipeCategory);
                        }
                        _logger.LogInformation("Added {CategoryCount} new categories", selectedCategories.Length);
                    }
                    
                    var changeCount = await _context.SaveChangesAsync();
                    _logger.LogInformation("Saved {ChangeCount} changes to database", changeCount);
                    
                    await transaction.CommitAsync();
                    _logger.LogInformation("Transaction committed successfully for recipe {RecipeId}", id);
                    
                    TempData["Message"] = "Recipe updated successfully!";
                    return RedirectToAction("Details", new { id });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Error updating recipe {RecipeId}", id);
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Critical error in Edit action for recipe {RecipeId}", id);
                TempData["Error"] = "Error updating recipe. Please check your input and try again.";
                
                await PrepareViewBagForEdit(id);
                return View(recipe);
            }
        }

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
                
                var userId = _userManager.GetUserId(User);
                if (recipe.CreatedById != userId && !User.IsInRole("Admin"))
                {
                    return Forbid();
                }
                
                if (!string.IsNullOrEmpty(recipe.ImagePath))
                {
                    DeleteRecipeImage(recipe.ImagePath);
                }
                
                _context.Recipes.Remove(recipe);
                await _context.SaveChangesAsync();
                
                TempData["Message"] = "Recipe deleted successfully!";
                return RedirectToAction("MyRecipes");
            }, "Error deleting recipe. Please try again later.", "MyRecipes");
        }

        [HttpGet]
        public async Task<IActionResult> GetRecipeSuggestions(string term)
        {
            if (string.IsNullOrEmpty(term))
            {
                return Json(new List<string>());
            }
            
            try
            {
                var lowerTerm = term.ToLower();
                var suggestions = await _context.Recipes
                    .Where(r => r.Title.ToLower().Contains(lowerTerm))
                    .Select(r => r.Title)
                    .Distinct()
                    .Take(10)
                    .ToListAsync();
                    
                return Json(suggestions);
            }
            catch
            {
                return Json(new List<string>());
            }
        }
        
        [HttpGet]
        public async Task<IActionResult> GetSearchSuggestions(string term)
        {
            if (string.IsNullOrEmpty(term))
            {
                return Json(new List<string>());
            }
            
            try
            {
                var lowerTerm = term.ToLower();
                
                var recipeSuggestions = await _context.Recipes
                    .Where(r => r.Title.ToLower().Contains(lowerTerm))
                    .Select(r => r.Title)
                    .Distinct()
                    .Take(5)
                    .ToListAsync();
                
                var ingredientSuggestions = await _context.Ingredients
                    .Where(i => i.Name.ToLower().Contains(lowerTerm))
                    .Select(i => i.Name)
                    .Distinct()
                    .Take(5)
                    .ToListAsync();
                
                var allSuggestions = recipeSuggestions
                    .Union(ingredientSuggestions)
                    .OrderBy(s => s)
                    .Take(10)
                    .ToList();
                
                return Json(allSuggestions);
            }
            catch
            {
                return Json(new List<string>());
            }
        }

        #region Helper Methods

        private void SetupViewDataForSearch(RecipeSearchViewModel searchModel)
        {
            ViewData["SearchTerm"] = searchModel.SearchTerm;
            ViewData["SearchType"] = searchModel.SearchType ?? "all";
            ViewData["Difficulty"] = searchModel.Difficulty;
            ViewData["SortBy"] = searchModel.SortBy ?? "newest";
            ViewData["MatchAllIngredients"] = searchModel.MatchAllIngredients.ToString().ToLower();
            ViewData["CategoryId"] = searchModel.CategoryId;
            ViewData["CreatedBy"] = searchModel.CreatedBy; 
        }

        private async Task SetupPaginationAndCategories(int page, int totalRecipes, int pageSize, int? categoryId)
        {
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalRecipes / (double)pageSize);
            ViewBag.Categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
            ViewBag.SelectedCategoryId = categoryId;
            
            ViewBag.Authors = await _context.Recipes
                .Include(r => r.CreatedBy)
                .Where(r => r.CreatedBy != null)
                .Select(r => new { Id = r.CreatedById, Name = r.CreatedBy.UserName })
                .Distinct()
                .OrderBy(a => a.Name)
                .ToListAsync();
        }

        private IQueryable<RecipeEntity> ApplyAdditionalFilters(IQueryable<RecipeEntity> query, RecipeSearchViewModel searchModel)
        {
            if (searchModel.Ingredients != null && searchModel.Ingredients.Any())
            {
                query = ApplyIngredientsFilter(query, searchModel.Ingredients, searchModel.MatchAllIngredients);
            }
            
            if (!string.IsNullOrEmpty(searchModel.Difficulty))
            {
                if (Enum.TryParse<DifficultyLevel>(searchModel.Difficulty, out var difficultyLevel))
                {
                    query = query.Where(r => r.Difficulty == difficultyLevel);
                }
            }
            
            if (!string.IsNullOrEmpty(searchModel.CreatedBy))
            {
                query = query.Where(r => r.CreatedById == searchModel.CreatedBy);
            }
            
            if (searchModel.MinServings.HasValue)
            {
                query = query.Where(r => r.Servings >= searchModel.MinServings.Value);
            }
            
            if (searchModel.MaxServings.HasValue)
            {
                query = query.Where(r => r.Servings <= searchModel.MaxServings.Value);
            }
            
            if (searchModel.MinPrepTime.HasValue)
            {
                query = query.Where(r => (r.PrepTimeMinutes + r.CookTimeMinutes) >= searchModel.MinPrepTime.Value);
            }
            
            if (searchModel.MaxPrepTime.HasValue)
            {
                query = query.Where(r => (r.PrepTimeMinutes + r.CookTimeMinutes) <= searchModel.MaxPrepTime.Value);
            }
            
            if (searchModel.CategoryId.HasValue)
            {
                query = query.Where(r => r.RecipeCategories.Any(rc => rc.CategoryId == searchModel.CategoryId.Value));
            }
            
            return query;
        }

        private async Task AddFavoriteStatusToViewBag()
        {
            if (User.Identity.IsAuthenticated)
            {
                var userId = _userManager.GetUserId(User);
                var favoriteRecipeIds = await _context.UserFavorites
                    .Where(uf => uf.UserId == userId)
                    .Select(uf => uf.RecipeId)
                    .ToListAsync();
            
                ViewBag.FavoriteRecipes = favoriteRecipeIds;
            }
        }

        private async Task SetUserSpecificDataForRecipe(RecipeEntity recipe)
        {
            if (User.Identity.IsAuthenticated)
            {
                var userId = _userManager.GetUserId(User);
                ViewBag.IsFavorited = await _context.UserFavorites
                    .AnyAsync(uf => uf.UserId == userId && uf.RecipeId == recipe.Id);
        
                ViewBag.UserRating = await _context.UserRatings
                    .Where(ur => ur.UserId == userId && ur.RecipeId == recipe.Id)
                    .Select(ur => ur.Rating)
                    .FirstOrDefaultAsync();
            }
            else
            {
                ViewBag.IsFavorited = false;
                ViewBag.UserRating = null;
            }
        }

        private async Task<string> SaveRecipeImage(IFormFile imageFile)
        {
            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
            var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "recipes");
            
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }
            
            var filePath = Path.Combine(uploadsFolder, fileName);
            
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(stream);
            }
            
            return "/images/recipes/" + fileName;
        }

        private void DeleteRecipeImage(string imagePath)
        {
            try
            {
                var fullPath = Path.Combine(_webHostEnvironment.WebRootPath, 
                    imagePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                if (System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting recipe image: {ImagePath}", imagePath);
            }
        }

        private async Task AddCategoriesToRecipe(int recipeId, int[]? categoryIds)
        {
            if (categoryIds == null || categoryIds.Length == 0)
            {
                return;
            }

            foreach (var categoryId in categoryIds)
            {
                _context.RecipeCategories.Add(new RecipeCategoryEntity
                {
                    RecipeId = recipeId,
                    CategoryId = categoryId
                });
            }
            
            await _context.SaveChangesAsync();
        }

        private async Task UpdateRecipeCategories(int recipeId, int[] selectedCategories)
        {
            var existingCategories = await _context.RecipeCategories
                .Where(rc => rc.RecipeId == recipeId)
                .ToListAsync();
                
            _context.RecipeCategories.RemoveRange(existingCategories);
            await _context.SaveChangesAsync();
            
            if (selectedCategories.Length > 0)
            {
                await AddCategoriesToRecipe(recipeId, selectedCategories);
            }
        }

        private async Task PrepareViewBagForEdit(int recipeId)
        {
            ViewBag.Categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
            ViewBag.SelectedCategories = await _context.RecipeCategories
                .Where(rc => rc.RecipeId == recipeId)
                .Select(rc => rc.CategoryId)
                .ToListAsync();
        }

        private IQueryable<RecipeEntity> ApplySearchFilters(IQueryable<RecipeEntity> query, RecipeSearchViewModel searchModel)
        {
            if (!string.IsNullOrEmpty(searchModel.SearchTerm))
            {
                var searchTerms = searchModel.SearchTerm.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(t => t.Trim().ToLower())
                    .Where(t => !string.IsNullOrEmpty(t))
                    .ToList();
                
                if (searchTerms.Any())
                {
                    query = ApplySearchTermFilters(query, searchModel.SearchType, searchTerms, searchModel.MatchAllIngredients);
                }
            }
            
            query = ApplyAdditionalFilters(query, searchModel);
            query = ApplySorting(query, searchModel.SortBy);
            
            return query;
        }

        private IQueryable<RecipeEntity> ApplySearchTermFilters(IQueryable<RecipeEntity> query, string searchType, 
            List<string> searchTerms, bool matchAllIngredients)
        {
            switch (searchType)
            {
                case "title":
                    foreach (var term in searchTerms)
                    {
                        query = query.Where(r => r.Title.ToLower().Contains(term));
                    }
                    break;
                    
                case "ingredients":
                    if (matchAllIngredients)
                    {
                        foreach (var term in searchTerms)
                        {
                            query = query.Where(r => r.Ingredients.ToLower().Contains(term));
                        }
                    }
                    else
                    {
                        query = query.Where(r => searchTerms.Any(term => r.Ingredients.ToLower().Contains(term)));
                    }
                    break;
                    
                case "all":
                default:
                    query = ApplyAllFieldsSearch(query, searchTerms, matchAllIngredients);
                    break;
            }
            
            return query;
        }

        private IQueryable<RecipeEntity> ApplyAllFieldsSearch(IQueryable<RecipeEntity> query, List<string> searchTerms, 
            bool matchAllIngredients)
        {
            if (searchTerms.Count == 1)
            {
                var term = searchTerms[0];
                return query.Where(r => 
                    r.Title.ToLower().Contains(term) || 
                    r.Description.ToLower().Contains(term) || 
                    r.Ingredients.ToLower().Contains(term));
            }
            
            if (matchAllIngredients)
            {
                var ingredientMatches = query;
                foreach (var term in searchTerms)
                {
                    ingredientMatches = ingredientMatches.Where(r => r.Ingredients.ToLower().Contains(term));
                }
                
                var titleDescMatches = query.Where(r => 
                    searchTerms.Any(term => 
                        r.Title.ToLower().Contains(term) || r.Description.ToLower().Contains(term)));
                
                var combinedQuery = ingredientMatches.Union(titleDescMatches);
                return combinedQuery;
            }
            
            return query.Where(r => 
                searchTerms.Any(term => 
                    r.Title.ToLower().Contains(term) || 
                    r.Description.ToLower().Contains(term) || 
                    r.Ingredients.ToLower().Contains(term)));
        }
        
        private IQueryable<RecipeEntity> ApplySorting(IQueryable<RecipeEntity> query, string sortBy)
        {
            return sortBy switch
            {
                "oldest" => query.OrderBy(r => r.CreatedDate),
                "fastest" => query.OrderBy(r => r.PrepTimeMinutes + r.CookTimeMinutes),
                "easiest" => query.OrderBy(r => r.Difficulty),
                "highestRated" => query.OrderByDescending(r => r.Ratings.Any() 
                    ? r.Ratings.Average(ur => ur.Rating) 
                    : 0),
                _ => query.OrderByDescending(r => r.CreatedDate)
            };
        }

        private IQueryable<RecipeEntity> ApplyIngredientsFilter(IQueryable<RecipeEntity> query, List<string> ingredients, bool matchAllIngredients)
        {
            if (matchAllIngredients)
            {
                foreach (var ingredient in ingredients)
                {
                    query = query.Where(r => r.Ingredients.ToLower().Contains(ingredient.ToLower()));
                }
            }
            else
            {
                var ingredientTerms = ingredients.Select(i => i.ToLower()).ToList();
                query = query.Where(r => 
                    ingredientTerms.Any(ingredient => r.Ingredients.ToLower().Contains(ingredient)));
            }
            
            return query;
        }
        
        #endregion
    }
}