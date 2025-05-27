using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MealStack.Infrastructure.Data;
using MealStack.Infrastructure.Data.Entities;
using System.Text;

namespace MealStack.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : BaseController
    {
        private readonly MealStackDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            MealStackDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ILogger<AdminController> logger)
            : base(userManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _logger = logger;
        }
        
        
        public IActionResult Index()
        {
            return View();
        }
        
        #region Manage Ingredients
        public async Task<IActionResult> ManageIngredients(
            string searchTerm, string category, string measurement, string createdBy, string hasDescription, string sortBy = "name", int page = 1)
        {
            int pageSize = 12;

            var ingredientsQuery = _context.Ingredients.Include(i => i.CreatedBy).AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                var searchPattern = $"%{searchTerm}%";
                ingredientsQuery = ingredientsQuery.Where(i =>
                    EF.Functions.Like(i.Name, searchPattern) ||
                    (i.Description != null && EF.Functions.Like(i.Description, searchPattern)) ||
                    (i.Category != null && EF.Functions.Like(i.Category, searchPattern)));
            }
            if (!string.IsNullOrEmpty(category))
            {
                ingredientsQuery = ingredientsQuery.Where(i => i.Category == category);
            }
            if (!string.IsNullOrEmpty(measurement))
            {
                ingredientsQuery = ingredientsQuery.Where(i => i.Measurement == measurement);
            }
            if (!string.IsNullOrEmpty(createdBy))
            {
                ingredientsQuery = ingredientsQuery.Where(i => i.CreatedById == createdBy);
            }
            if (!string.IsNullOrEmpty(hasDescription))
            {
                if (bool.TryParse(hasDescription, out bool hasDesc))
                {
                    if (hasDesc)
                        ingredientsQuery = ingredientsQuery.Where(i => !string.IsNullOrEmpty(i.Description));
                    else
                        ingredientsQuery = ingredientsQuery.Where(i => string.IsNullOrEmpty(i.Description));
                }
            }
            ingredientsQuery = sortBy switch
            {
                "name_desc" => ingredientsQuery.OrderByDescending(i => i.Name),
                "category" => ingredientsQuery.OrderBy(i => i.Category).ThenBy(i => i.Name),
                "measurement" => ingredientsQuery.OrderBy(i => i.Measurement).ThenBy(i => i.Name),
                "newest" => ingredientsQuery.OrderByDescending(i => i.CreatedDate),
                "author" => ingredientsQuery.OrderBy(i => i.CreatedBy.UserName).ThenBy(i => i.Name),
                _ => ingredientsQuery.OrderBy(i => i.Name)
            };

            var totalIngredients = await ingredientsQuery.CountAsync();

            var ingredients = await ingredientsQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalIngredients / (double)pageSize);
            ViewBag.TotalItems = totalIngredients;
            ViewBag.TotalIngredients = totalIngredients;
            ViewData["SearchTerm"] = searchTerm;
            ViewData["Category"] = category;
            ViewData["Measurement"] = measurement;
            ViewData["CreatedBy"] = createdBy;
            ViewData["HasDescription"] = hasDescription;
            ViewData["SortBy"] = sortBy;

            ViewBag.Categories = await _context.Ingredients
                .Where(i => !string.IsNullOrEmpty(i.Category))
                .Select(i => i.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            ViewBag.Measurements = await _context.Ingredients
                .Where(i => !string.IsNullOrEmpty(i.Measurement))
                .Select(i => i.Measurement)
                .Distinct()
                .OrderBy(m => m)
                .ToListAsync();

            ViewBag.Authors = await _context.Ingredients
                .Where(i => i.CreatedBy != null)
                .Select(i => new { Id = i.CreatedById, Name = i.CreatedBy.UserName })
                .Distinct()
                .OrderBy(a => a.Name)
                .ToListAsync();

            return View(ingredients); 
        }
        #endregion
        
        #region Manage Recipes
        public async Task<IActionResult> Users()
        {
            var users = await _userManager.Users.ToListAsync();
            var userViewModels = new List<UserViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var recipesCount = await _context.Recipes.CountAsync(r => r.CreatedById == user.Id);

                userViewModels.Add(new UserViewModel
                {
                    Id = user.Id,
                    UserName = user.UserName,
                    Email = user.Email,
                    Roles = roles.ToList(),
                    RecipesCount = recipesCount,
                    EmailConfirmed = user.EmailConfirmed
                });
            }

            return View(userViewModels);
        }
        #endregion

        #region Manage Meal Plans
        public async Task<IActionResult> ManageMealPlans()
        {
            var mealPlans = await _context.MealPlans
                .Include(mp => mp.User)
                .Include(mp => mp.MealItems)
                .OrderByDescending(mp => mp.CreatedDate)
                .ToListAsync();

            return View(mealPlans);
        }
        #endregion
        
        public async Task<IActionResult> ManageCategories()
        {
            var categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
            return View(categories);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleUserRole(string userId, string roleName)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(roleName))
            {
                TempData["Error"] = "User ID and role name are required.";
                return RedirectToAction(nameof(Users));
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction(nameof(Users));
            }

            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                TempData["Error"] = $"Role '{roleName}' does not exist.";
                return RedirectToAction(nameof(Users));
            }

            if (await _userManager.IsInRoleAsync(user, roleName))
            {
                await _userManager.RemoveFromRoleAsync(user, roleName);
                TempData["Message"] = $"Role '{roleName}' removed from user {user.UserName}.";
            }
            else
            {
                await _userManager.AddToRoleAsync(user, roleName);
                TempData["Message"] = $"Role '{roleName}' added to user {user.UserName}.";
            }

            return RedirectToAction(nameof(Users));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateUserUsername(string userId, string username)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(username))
            {
                TempData["Error"] = "User ID and username are required.";
                return RedirectToAction(nameof(Users));
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction(nameof(Users));
            }

            var existingUser = await _userManager.FindByNameAsync(username);
            if (existingUser != null && existingUser.Id != userId)
            {
                TempData["Error"] = "Username is already taken.";
                return RedirectToAction(nameof(Users));
            }

            user.UserName = username;
            user.NormalizedUserName = _userManager.NormalizeName(username);

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                TempData["Message"] = $"Username updated to '{username}' successfully.";
            }
            else
            {
                TempData["Error"] = "Failed to update username: " + string.Join(", ", result.Errors.Select(e => e.Description));
            }

            return RedirectToAction(nameof(Users));
        }

        public async Task<IActionResult> ManageRecipes(string searchTerm, string difficulty, string timeFilter, string sortBy, int? categoryId, int page = 1)
        {
            return await TryExecuteAsync(async () =>
            {
                const int pageSize = 12;
                
                var recipesQuery = _context.Recipes
                    .Include(r => r.CreatedBy)
                    .Include(r => r.RecipeCategories).ThenInclude(rc => rc.Category)
                    .Include(r => r.Ratings)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    recipesQuery = recipesQuery.Where(r => 
                        r.Title.Contains(searchTerm) || 
                        r.Description.Contains(searchTerm) || 
                        r.Ingredients.Contains(searchTerm));
                }

                if (Enum.TryParse(difficulty, out DifficultyLevel difficultyLevel))
                {
                    recipesQuery = recipesQuery.Where(r => r.Difficulty == difficultyLevel);
                }

                if (int.TryParse(timeFilter, out int minutes))
                {
                    recipesQuery = recipesQuery.Where(r => (r.PrepTimeMinutes + r.CookTimeMinutes) <= minutes);
                }

                if (categoryId.HasValue)
                {
                    recipesQuery = recipesQuery.Where(r => r.RecipeCategories.Any(rc => rc.CategoryId == categoryId));
                }

                recipesQuery = sortBy switch
                {
                    "oldest" => recipesQuery.OrderBy(r => r.CreatedDate),
                    "title" => recipesQuery.OrderBy(r => r.Title),
                    "difficulty" => recipesQuery.OrderBy(r => r.Difficulty),
                    "preptime" => recipesQuery.OrderBy(r => r.PrepTimeMinutes + r.CookTimeMinutes),
                    "rating" => recipesQuery.OrderByDescending(r => r.Ratings.Any() ? r.Ratings.Average(ur => ur.Rating) : 0),
                    _ => recipesQuery.OrderByDescending(r => r.CreatedDate)
                };

                var totalRecipes = await recipesQuery.CountAsync();
                var recipes = await recipesQuery
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
                
                ViewBag.Categories = categories;
                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = (int)Math.Ceiling(totalRecipes / (double)pageSize);
                ViewBag.TotalItems = totalRecipes;
                ViewBag.ItemsPerPage = pageSize;
                ViewBag.SelectedCategoryId = categoryId;

                ViewData["SearchTerm"] = searchTerm;
                ViewData["Difficulty"] = difficulty;
                ViewData["TimeFilter"] = timeFilter;
                ViewData["SortBy"] = sortBy;

                var categoriesForJs = categories.Select(c => new { id = c.Id, name = c.Name }).ToList();
                ViewData["CategoriesJson"] = System.Text.Json.JsonSerializer.Serialize(categoriesForJs);

                return View(recipes);
            }, "Error loading recipes management. Please try again later.");
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkAction(string action, string selectedIds, BulkActionOptions options)
        {
            return await TryExecuteAsync(async () =>
            {
                if (string.IsNullOrEmpty(action) || string.IsNullOrEmpty(selectedIds))
                {
                    return Json(new { success = false, message = "Invalid bulk action parameters" });
                }

                var recipeIds = selectedIds.Split(',').Select(int.Parse).ToList();
                var recipes = await _context.Recipes
                    .Include(r => r.RecipeCategories)
                    .Include(r => r.Ratings)
                    .Include(r => r.CreatedBy)
                    .Where(r => recipeIds.Contains(r.Id))
                    .ToListAsync();

                if (!recipes.Any())
                {
                    return Json(new { success = false, message = "No recipes found for the selected IDs" });
                }

                try
                {
                    switch (action.ToLower())
                    {
                        case "addcategories":
                            if (options.CategoryIds != null && options.CategoryIds.Any())
                            {
                                await AddCategoriesToRecipes(recipes, options.CategoryIds);
                                await _context.SaveChangesAsync();
                                return Json(new { success = true, message = $"Added categories to {recipes.Count} recipes" });
                            }
                            return Json(new { success = false, message = "No categories selected" });

                        case "removecategories":
                            if (options.CategoryIds != null && options.CategoryIds.Any())
                            {
                                await RemoveCategoriesFromRecipes(recipes, options.CategoryIds);
                                await _context.SaveChangesAsync();
                                return Json(new { success = true, message = $"Removed categories from {recipes.Count} recipes" });
                            }
                            return Json(new { success = false, message = "No categories selected" });

                        case "changedifficulty":
                            if (Enum.TryParse<DifficultyLevel>(options.NewDifficulty, out var newDifficulty))
                            {
                                await ChangeDifficultyForRecipes(recipes, newDifficulty);
                                await _context.SaveChangesAsync();
                                return Json(new { success = true, message = $"Changed difficulty for {recipes.Count} recipes to {newDifficulty}" });
                            }
                            return Json(new { success = false, message = "Invalid difficulty level" });

                        case "export":
                            return await ExportRecipes(recipes, options.ExportFormat ?? "csv");

                        case "delete":
                            await DeleteRecipes(recipes);
                            await _context.SaveChangesAsync();
                            return Json(new { success = true, message = $"Successfully deleted {recipes.Count} recipes" });

                        default:
                            return Json(new { success = false, message = "Unknown bulk action: " + action });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error executing bulk action {Action} on {Count} recipes", action, recipes.Count);
                    return Json(new { success = false, message = $"Error executing bulk action: {ex.Message}" });
                }
            }, "Error executing bulk action. Please try again later.");
        }
        
        public class BulkActionOptions
        {
            public List<int> CategoryIds { get; set; } = new();
            public string NewDifficulty { get; set; }
            public string ExportFormat { get; set; }
            public bool IncludeImages { get; set; }
        }

        private async Task AddCategoriesToRecipes(List<RecipeEntity> recipes, List<int> categoryIds)
        {
            foreach (var recipe in recipes)
            {
                foreach (var categoryId in categoryIds)
                {
                    if (!recipe.RecipeCategories.Any(rc => rc.CategoryId == categoryId))
                    {
                        _context.RecipeCategories.Add(new RecipeCategoryEntity
                        {
                            RecipeId = recipe.Id,
                            CategoryId = categoryId
                        });
                    }
                }
            }
        }

        private async Task RemoveCategoriesFromRecipes(List<RecipeEntity> recipes, List<int> categoryIds)
        {
            var categoriesToRemove = _context.RecipeCategories
                .Where(rc => recipes.Select(r => r.Id).Contains(rc.RecipeId) && categoryIds.Contains(rc.CategoryId));
            
            _context.RecipeCategories.RemoveRange(categoriesToRemove);
        }

        private async Task ChangeDifficultyForRecipes(List<RecipeEntity> recipes, DifficultyLevel newDifficulty)
        {
            foreach (var recipe in recipes)
            {
                recipe.Difficulty = newDifficulty;
                recipe.UpdatedDate = DateTime.UtcNow;
            }
        }

        private async Task<IActionResult> ExportRecipes(List<RecipeEntity> recipes, string format, bool includeImages = false)
        {
            switch (format.ToLower())
            {
                case "csv":
                    var csv = GenerateRecipesCsv(recipes);
                    return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", "recipes_export.csv");
                
                case "json":
                    var json = System.Text.Json.JsonSerializer.Serialize(recipes.Select(r => new
                    {
                        r.Id,
                        r.Title,
                        r.Description,
                        r.Difficulty,
                        r.PrepTimeMinutes,
                        r.CookTimeMinutes,
                        r.Servings,
                        Author = r.CreatedBy?.UserName,
                        Categories = r.RecipeCategories?.Select(rc => rc.Category?.Name)
                    }), new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                    return File(System.Text.Encoding.UTF8.GetBytes(json), "application/json", "recipes_export.json");
                
                default:
                    return Json(new { success = false, message = "Unsupported export format" });
            }
        }

        private string GenerateRecipesCsv(List<RecipeEntity> recipes)
        {
            var csv = new System.Text.StringBuilder();
            csv.AppendLine("ID,Title,Description,Difficulty,PrepTime,CookTime,Servings,Author,Categories");
            
            foreach (var recipe in recipes)
            {
                var categories = string.Join(";", recipe.RecipeCategories?.Select(rc => rc.Category?.Name) ?? new string[0]);
                csv.AppendLine($"{recipe.Id},\"{recipe.Title}\",\"{recipe.Description}\",{recipe.Difficulty},{recipe.PrepTimeMinutes},{recipe.CookTimeMinutes},{recipe.Servings},\"{recipe.CreatedBy?.UserName}\",\"{categories}\"");
            }
            
            return csv.ToString();
        }

        private async Task DeleteRecipes(List<RecipeEntity> recipes)
        {
            _context.Recipes.RemoveRange(recipes);
        }
        
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMealPlan(int id)
        {
            try
            {
                var mealPlan = await _context.MealPlans
                    .Include(mp => mp.MealItems)
                    .Include(mp => mp.ShoppingItems)
                    .FirstOrDefaultAsync(mp => mp.Id == id);

                if (mealPlan == null)
                {
                    TempData["Error"] = "Meal plan not found.";
                    return RedirectToAction(nameof(ManageMealPlans));
                }

                _context.MealPlans.Remove(mealPlan);
                await _context.SaveChangesAsync();

                TempData["Message"] = $"Meal plan '{mealPlan.Name}' deleted successfully.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error deleting meal plan: {ex.Message}";
            }

            return RedirectToAction(nameof(ManageMealPlans));
        }
    }
    
    
    public class UserViewModel
    {
        public string Id { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public List<string> Roles { get; set; } = new();
        public int RecipesCount { get; set; }
        public bool EmailConfirmed { get; set; }
    }
}