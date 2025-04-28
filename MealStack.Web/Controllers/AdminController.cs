using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MealStack.Infrastructure.Data;
using MealStack.Infrastructure.Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MealStack.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : BaseController
    {
        private readonly MealStackDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AdminController(
            MealStackDbContext context, 
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
            : base(userManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> Users()
        {
            var users = await _userManager.Users.ToListAsync();
            var userViewModels = new List<UserViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var recipes = await _context.Recipes
                    .Where(r => r.CreatedById == user.Id)
                    .CountAsync();

                userViewModels.Add(new UserViewModel
                {
                    Id = user.Id,
                    UserName = user.UserName,
                    Email = user.Email,
                    Roles = roles.ToList(),
                    RecipesCount = recipes,
                    EmailConfirmed = user.EmailConfirmed
                });
            }

            return View(userViewModels);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
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

            // Check if role exists
            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                TempData["Error"] = $"Role '{roleName}' does not exist.";
                return RedirectToAction(nameof(Users));
            }

            // Toggle role
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
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateUserUsername(string userId, string username)
        {
            if (string.IsNullOrEmpty(userId))
            {
                TempData["Error"] = "User ID is required.";
                return RedirectToAction(nameof(Users));
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction(nameof(Users));
            }

            // Validate username
            if (string.IsNullOrEmpty(username) || username.Length < 3 || username.Length > 10)
            {
                TempData["Error"] = "Username must be between 3 and 10 characters.";
                return RedirectToAction(nameof(Users));
            }

            if (!System.Text.RegularExpressions.Regex.IsMatch(username, @"^[a-zA-Z]+$"))
            {
                TempData["Error"] = "Username can only contain letters (no numbers or special characters).";
                return RedirectToAction(nameof(Users));
            }

            // Check if username is already taken
            var existingUser = await _userManager.FindByNameAsync(username);
            if (existingUser != null && existingUser.Id != userId)
            {
                TempData["Error"] = "This username is already taken.";
                return RedirectToAction(nameof(Users));
            }

            // Update username
            user.UserName = username;
            user.NormalizedUserName = _userManager.NormalizeName(username);
            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                TempData["Message"] = $"Username for {user.Email} updated to '{username}'.";
            }
            else
            {
                TempData["Error"] = "Failed to update username: " + string.Join(", ", result.Errors.Select(e => e.Description));
            }

            return RedirectToAction(nameof(Users));
        }

        // Updated ManageRecipes method with filtering support
        public async Task<IActionResult> ManageRecipes(string searchTerm, string difficulty, string timeFilter, string sortBy, int? categoryId)
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApplyBulkCategories(int[] selectedRecipes, int[] selectedCategories)
        {
            if (selectedRecipes == null || selectedRecipes.Length == 0)
            {
                TempData["Error"] = "No recipes were selected.";
                return RedirectToAction(nameof(ManageRecipes));
            }
            
            if (selectedCategories == null || selectedCategories.Length == 0)
            {
                TempData["Error"] = "No categories were selected.";
                return RedirectToAction(nameof(ManageRecipes));
            }
            
            try
            {
                // Process each selected recipe
                foreach (var recipeId in selectedRecipes)
                {
                    var recipe = await _context.Recipes
                        .Include(r => r.RecipeCategories)
                        .FirstOrDefaultAsync(r => r.Id == recipeId);
                        
                    if (recipe != null)
                    {
                        // Add new categories (avoiding duplicates)
                        foreach (var categoryId in selectedCategories)
                        {
                            // Check if this category is already assigned to the recipe
                            bool categoryExists = recipe.RecipeCategories
                                .Any(rc => rc.CategoryId == categoryId);
                                
                            if (!categoryExists)
                            {
                                _context.Add(new RecipeCategoryEntity
                                {
                                    RecipeId = recipe.Id,
                                    CategoryId = categoryId
                                });
                            }
                        }
                    }
                }
                
                await _context.SaveChangesAsync();
                
                TempData["Message"] = $"Categories successfully applied to {selectedRecipes.Length} recipe(s).";
                return RedirectToAction(nameof(ManageRecipes));
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"An error occurred: {ex.Message}";
                return RedirectToAction(nameof(ManageRecipes));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveBulkCategories(int[] selectedRecipes, int[] selectedCategories)
        {
            if (selectedRecipes == null || selectedRecipes.Length == 0)
            {
                TempData["Error"] = "No recipes were selected.";
                return RedirectToAction(nameof(ManageRecipes));
            }
            
            if (selectedCategories == null || selectedCategories.Length == 0)
            {
                TempData["Error"] = "No categories were selected.";
                return RedirectToAction(nameof(ManageRecipes));
            }
            
            try
            {
                // Find all recipe-category relationships to remove
                var recipeCategoryRelations = await _context.RecipeCategories
                    .Where(rc => selectedRecipes.Contains(rc.RecipeId) && selectedCategories.Contains(rc.CategoryId))
                    .ToListAsync();
                    
                // Remove them all
                _context.RecipeCategories.RemoveRange(recipeCategoryRelations);
                await _context.SaveChangesAsync();
                
                TempData["Message"] = $"Categories successfully removed from {selectedRecipes.Length} recipe(s).";
                return RedirectToAction(nameof(ManageRecipes));
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"An error occurred: {ex.Message}";
                return RedirectToAction(nameof(ManageRecipes));
            }
        }

        // Redirect old ManageIngredients action to Ingredient/Index
        public IActionResult ManageIngredients()
        {
            return RedirectToAction("Index", "Ingredient");
        }

        // Categories action remains as a menu page that links to the Category controller
        public IActionResult Categories()
        {
            return View();
        }
    }

    public class UserViewModel
    {
        public string Id { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public List<string> Roles { get; set; } = new List<string>();
        public int RecipesCount { get; set; }
        public bool EmailConfirmed { get; set; }
        public string DisplayName { get; set; }
    }
}