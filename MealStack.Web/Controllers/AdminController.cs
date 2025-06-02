using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using MealStack.Infrastructure.Data;
using MealStack.Infrastructure.Data.Entities;
using Microsoft.AspNetCore.Identity;
using System.Text.Json;

namespace MealStack.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly MealStackDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            MealStackDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<AdminController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        public IActionResult Index()
        {
            ViewBag.TotalUsers = _context.Users.Count();
            ViewBag.TotalRecipes = _context.Recipes.Count();
            ViewBag.TotalCategories = _context.Categories.Count();
            ViewBag.TotalIngredients = _context.Ingredients.Count();
            ViewBag.TotalMealPlans = _context.MealPlans.Count();

            return View();
        }

        public async Task<IActionResult> ManageRecipes(
            string searchTerm = "",
            string sortBy = "newest",
            int? categoryId = null,
            string difficulty = "",
            string timeFilter = "",
            string ingredients = "",
            int page = 1,
            int pageSize = 20)
        {
            try
            {
                var query = _context.Recipes
                    .Include(r => r.CreatedBy)
                    .Include(r => r.RecipeCategories)
                        .ThenInclude(rc => rc.Category)
                    .Include(r => r.Ratings)
                    .AsQueryable();

                // Apply filters
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    query = query.Where(r => r.Title.Contains(searchTerm) || 
                                           r.Description.Contains(searchTerm) ||
                                           r.Instructions.Contains(searchTerm) ||
                                           r.Ingredients.Contains(searchTerm));
                }

                if (categoryId.HasValue)
                {
                    query = query.Where(r => r.RecipeCategories.Any(rc => rc.CategoryId == categoryId.Value));
                }

                if (!string.IsNullOrEmpty(difficulty))
                {
                    if (Enum.TryParse<DifficultyLevel>(difficulty, out var difficultyEnum))
                    {
                        query = query.Where(r => r.Difficulty == difficultyEnum);
                    }
                }

                if (!string.IsNullOrEmpty(timeFilter) && int.TryParse(timeFilter, out var maxTime))
                {
                    query = query.Where(r => (r.PrepTimeMinutes + r.CookTimeMinutes) <= maxTime);
                }

                if (!string.IsNullOrEmpty(ingredients))
                {
                    var ingredientList = ingredients.Split(',')
                        .Select(i => i.Trim())
                        .Where(i => !string.IsNullOrEmpty(i));

                    foreach (var ingredient in ingredientList)
                    {
                        query = query.Where(r => r.Ingredients.Contains(ingredient));
                    }
                }

                // Apply sorting
                query = sortBy.ToLower() switch
                {
                    "oldest" => query.OrderBy(r => r.CreatedDate),
                    "title" => query.OrderBy(r => r.Title),
                    "difficulty" => query.OrderBy(r => r.Difficulty),
                    "rating" => query.OrderByDescending(r => r.Ratings.Any() ? r.Ratings.Average(rt => rt.Rating) : 0),
                    _ => query.OrderByDescending(r => r.CreatedDate)
                };

                // Get total count before pagination
                var totalItems = await query.CountAsync();
                var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

                // Apply pagination
                var recipes = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .AsSplitQuery()
                    .ToListAsync();

                // Get categories for filter
                var categories = await _context.Categories
                    .OrderBy(c => c.Name)
                    .ToListAsync();

                // Set ViewBag/ViewData
                ViewBag.Categories = categories;
                ViewBag.SelectedCategoryId = categoryId;
                ViewData["SearchTerm"] = searchTerm;
                ViewData["SortBy"] = sortBy;
                ViewData["Difficulty"] = difficulty;
                ViewData["TimeFilter"] = timeFilter;
                ViewData["Ingredients"] = ingredients;
                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;
                ViewBag.TotalItems = totalItems;
                ViewBag.ItemsPerPage = pageSize;

                return View(recipes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while managing recipes");
                TempData["Error"] = "An error occurred while loading recipes. Please try again.";
                return View(new List<RecipeEntity>());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkAction(string bulkAction, string selectedIds, 
            int[]? categoryIds = null, string? newDifficulty = null, string? exportFormat = null)
        {
            try
            {
                if (string.IsNullOrEmpty(bulkAction) || string.IsNullOrEmpty(selectedIds))
                {
                    return Json(new { success = false, message = "Invalid bulk action request." });
                }

                var recipeIds = selectedIds.Split(',')
                    .Where(id => int.TryParse(id, out _))
                    .Select(int.Parse)
                    .ToList();

                if (!recipeIds.Any())
                {
                    return Json(new { success = false, message = "No valid recipe IDs provided." });
                }

                var recipes = await _context.Recipes
                    .Include(r => r.RecipeCategories)
                    .Where(r => recipeIds.Contains(r.Id))
                    .ToListAsync();

                switch (bulkAction.ToLower())
                {
                    case "addcategories":
                        await AddCategoriesToRecipes(recipes, categoryIds);
                        break;

                    case "removecategories":
                        await RemoveCategoriesFromRecipes(recipes, categoryIds);
                        break;

                    case "changedifficulty":
                        await ChangeDifficulty(recipes, newDifficulty);
                        break;

                    case "export":
                        return await ExportRecipes(recipes, exportFormat);

                    case "delete":
                        await DeleteRecipes(recipes);
                        break;

                    default:
                        return Json(new { success = false, message = "Unknown bulk action." });
                }

                return Json(new { success = true, message = $"Bulk action completed successfully for {recipes.Count} recipes." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during bulk action: {Action}", bulkAction);
                return Json(new { success = false, message = "An error occurred while processing the bulk action." });
            }
        }

        private async Task AddCategoriesToRecipes(List<RecipeEntity> recipes, int[]? categoryIds)
        {
            if (categoryIds == null || !categoryIds.Any()) return;

            foreach (var recipe in recipes)
            {
                foreach (var categoryId in categoryIds)
                {
                    if (!recipe.RecipeCategories.Any(rc => rc.CategoryId == categoryId))
                    {
                        recipe.RecipeCategories.Add(new RecipeCategoryEntity
                        {
                            RecipeId = recipe.Id,
                            CategoryId = categoryId
                        });
                    }
                }
            }

            await _context.SaveChangesAsync();
        }

        private async Task RemoveCategoriesFromRecipes(List<RecipeEntity> recipes, int[]? categoryIds)
        {
            if (categoryIds == null || !categoryIds.Any()) return;

            foreach (var recipe in recipes)
            {
                var categoriesToRemove = recipe.RecipeCategories
                    .Where(rc => categoryIds.Contains(rc.CategoryId))
                    .ToList();

                foreach (var category in categoriesToRemove)
                {
                    recipe.RecipeCategories.Remove(category);
                }
            }

            await _context.SaveChangesAsync();
        }

        private async Task ChangeDifficulty(List<RecipeEntity> recipes, string? newDifficulty)
        {
            if (string.IsNullOrEmpty(newDifficulty) || 
                !Enum.TryParse<DifficultyLevel>(newDifficulty, out var difficulty))
                return;

            foreach (var recipe in recipes)
            {
                recipe.Difficulty = difficulty;
                recipe.UpdatedDate = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
        }

        private async Task<IActionResult> ExportRecipes(List<RecipeEntity> recipes, string? format)
        {
            // This is a placeholder - implement actual export logic based on format
            // You might want to create CSV, JSON, or PDF exports here
            
            switch (format?.ToLower())
            {
                case "csv":
                    return await ExportToCsv(recipes);
                case "json":
                    return await ExportToJson(recipes);
                case "pdf":
                    return await ExportToPdf(recipes);
                default:
                    return Json(new { success = false, message = "Unsupported export format." });
            }
        }

        private async Task<IActionResult> ExportToCsv(List<RecipeEntity> recipes)
        {
            // Create CSV content
            var csvContent = new StringBuilder();
            csvContent.AppendLine("Title,Description,Difficulty,PrepTime,CookTime,Servings,CreatedDate");

            foreach (var recipe in recipes)
            {
                csvContent.AppendLine($"\"{recipe.Title}\",\"{recipe.Description}\",\"{recipe.Difficulty}\",{recipe.PrepTimeMinutes},{recipe.CookTimeMinutes},{recipe.Servings},\"{recipe.CreatedDate:yyyy-MM-dd}\"");
            }

            var bytes = Encoding.UTF8.GetBytes(csvContent.ToString());
            return File(bytes, "text/csv", $"recipes_export_{DateTime.Now:yyyyMMdd}.csv");
        }

        private async Task<IActionResult> ExportToJson(List<RecipeEntity> recipes)
        {
            // Create a simplified version without circular references
            var exportData = recipes.Select(r => new
            {
                r.Id,
                r.Title,
                r.Description,
                r.Instructions,
                r.Ingredients,
                r.PrepTimeMinutes,
                r.CookTimeMinutes,
                r.Servings,
                r.Difficulty,
                r.CreatedDate,
                Categories = r.RecipeCategories?.Select(rc => rc.Category?.Name).ToArray() ?? new string[0]
            }).ToList();

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var json = JsonSerializer.Serialize(exportData, options);
            var bytes = Encoding.UTF8.GetBytes(json);
            
            return File(bytes, "application/json", $"recipes_export_{DateTime.Now:yyyyMMdd}.json");
        }

        private async Task<IActionResult> ExportToPdf(List<RecipeEntity> recipes)
        {
            // Placeholder for PDF export - you'd need a PDF library like iTextSharp or similar
            return Json(new { success = false, message = "PDF export not yet implemented." });
        }

        private async Task DeleteRecipes(List<RecipeEntity> recipes)
        {
            // Remove related data first
            foreach (var recipe in recipes)
            {
                // Remove ratings
                var ratings = await _context.UserRatings
                    .Where(r => r.RecipeId == recipe.Id)
                    .ToListAsync();
                _context.UserRatings.RemoveRange(ratings);

                // Remove favorites
                var favorites = await _context.UserFavorites
                    .Where(f => f.RecipeId == recipe.Id)
                    .ToListAsync();
                _context.UserFavorites.RemoveRange(favorites);

                // Remove from meal plans
                var mealItems = await _context.MealPlanItems
                    .Where(mi => mi.RecipeId == recipe.Id)
                    .ToListAsync();
                _context.MealPlanItems.RemoveRange(mealItems);
            }

            // Remove recipes
            _context.Recipes.RemoveRange(recipes);
            await _context.SaveChangesAsync();
        }

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
                    UserName = user.UserName ?? "Unknown",
                    Email = user.Email ?? "No email",
                    EmailConfirmed = user.EmailConfirmed,
                    Roles = roles.ToList(),
                    RecipesCount = recipesCount
                });
            }

            return View(userViewModels);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleUserRole(string userId, string roleName)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    TempData["Error"] = "User not found.";
                    return RedirectToAction(nameof(Users));
                }

                var isInRole = await _userManager.IsInRoleAsync(user, roleName);

                if (isInRole)
                {
                    await _userManager.RemoveFromRoleAsync(user, roleName);
                    TempData["Message"] = $"Removed {roleName} role from {user.UserName}";
                }
                else
                {
                    await _userManager.AddToRoleAsync(user, roleName);
                    TempData["Message"] = $"Added {roleName} role to {user.UserName}";
                }

                return RedirectToAction(nameof(Users));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling user role");
                TempData["Error"] = "An error occurred while updating user roles.";
                return RedirectToAction(nameof(Users));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateUserUsername(string userId, string username)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(username) || username.Length < 3 || username.Length > 10)
                {
                    TempData["Error"] = "Username must be between 3-10 characters.";
                    return RedirectToAction(nameof(Users));
                }

                if (!System.Text.RegularExpressions.Regex.IsMatch(username, @"^[a-zA-Z]+$"))
                {
                    TempData["Error"] = "Username can only contain letters.";
                    return RedirectToAction(nameof(Users));
                }

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    TempData["Error"] = "User not found.";
                    return RedirectToAction(nameof(Users));
                }

                // Check if username is already taken
                var existingUser = await _userManager.FindByNameAsync(username);
                if (existingUser != null && existingUser.Id != userId)
                {
                    TempData["Error"] = "Username is already taken.";
                    return RedirectToAction(nameof(Users));
                }

                user.UserName = username;
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating username");
                TempData["Error"] = "An error occurred while updating the username.";
                return RedirectToAction(nameof(Users));
            }
        }

        public async Task<IActionResult> ManageIngredients(
            string searchTerm = "",
            string sortBy = "name",
            string category = "",
            string measurement = "",
            string createdBy = "",
            string hasDescription = "",
            int page = 1,
            int pageSize = 20)
        {
            try
            {
                var query = _context.Ingredients
                    .Include(i => i.CreatedBy)
                    .AsQueryable();

                // Apply filters
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    query = query.Where(i => i.Name.Contains(searchTerm) || 
                                           (i.Description != null && i.Description.Contains(searchTerm)));
                }

                if (!string.IsNullOrEmpty(category))
                {
                    query = query.Where(i => i.Category == category);
                }

                if (!string.IsNullOrEmpty(measurement))
                {
                    query = query.Where(i => i.Measurement == measurement);
                }

                if (!string.IsNullOrEmpty(createdBy))
                {
                    query = query.Where(i => i.CreatedById == createdBy);
                }

                if (!string.IsNullOrEmpty(hasDescription))
                {
                    if (hasDescription.ToLower() == "true")
                    {
                        query = query.Where(i => !string.IsNullOrEmpty(i.Description));
                    }
                    else if (hasDescription.ToLower() == "false")
                    {
                        query = query.Where(i => string.IsNullOrEmpty(i.Description));
                    }
                }

                // Apply sorting
                query = sortBy.ToLower() switch
                {
                    "name_desc" => query.OrderByDescending(i => i.Name),
                    "category" => query.OrderBy(i => i.Category),
                    "measurement" => query.OrderBy(i => i.Measurement),
                    "newest" => query.OrderByDescending(i => i.CreatedDate),
                    "author" => query.OrderBy(i => i.CreatedBy.UserName),
                    _ => query.OrderBy(i => i.Name)
                };

                // Get total count before pagination
                var totalItems = await query.CountAsync();
                var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

                // Apply pagination
                var ingredients = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                // Get filter options
                var categories = await _context.Ingredients
                    .Where(i => !string.IsNullOrEmpty(i.Category))
                    .Select(i => i.Category)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToListAsync();

                var measurements = await _context.Ingredients
                    .Where(i => !string.IsNullOrEmpty(i.Measurement))
                    .Select(i => i.Measurement)
                    .Distinct()
                    .OrderBy(m => m)
                    .ToListAsync();

                var authors = await _context.Ingredients
                    .Include(i => i.CreatedBy)
                    .Where(i => i.CreatedBy != null)
                    .Select(i => new { i.CreatedById, Name = i.CreatedBy.UserName })
                    .Distinct()
                    .OrderBy(a => a.Name)
                    .ToListAsync();

                // Set ViewBag/ViewData
                ViewBag.Categories = categories;
                ViewBag.Measurements = measurements;
                ViewBag.Authors = authors.Select(a => new { Id = a.CreatedById, Name = a.Name }).ToList();
                ViewData["SearchTerm"] = searchTerm;
                ViewData["SortBy"] = sortBy;
                ViewData["Category"] = category;
                ViewData["Measurement"] = measurement;
                ViewData["CreatedBy"] = createdBy;
                ViewData["HasDescription"] = hasDescription;
                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;
                ViewBag.TotalItems = totalItems;
                ViewBag.ItemsPerPage = pageSize;
                ViewBag.TotalIngredients = totalItems;

                return View(ingredients);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while managing ingredients");
                TempData["Error"] = "An error occurred while loading ingredients. Please try again.";
                return View(new List<IngredientEntity>());
            }
        }

        public async Task<IActionResult> ManageMealPlans()
        {
            try
            {
                var mealPlans = await _context.MealPlans
                    .Include(mp => mp.User)
                    .Include(mp => mp.MealItems)
                    .OrderByDescending(mp => mp.CreatedDate)
                    .ToListAsync();

                return View(mealPlans);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while loading meal plans");
                TempData["Error"] = "An error occurred while loading meal plans. Please try again.";
                return View(new List<MealPlanEntity>());
            }
        }
    }

    // ViewModel for user management
    public class UserViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool EmailConfirmed { get; set; }
        public List<string> Roles { get; set; } = new();
        public int RecipesCount { get; set; }
    }
}