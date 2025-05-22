using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MealStack.Infrastructure.Data;
using MealStack.Infrastructure.Data.Entities;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

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

        public async Task<IActionResult> ManageMealPlans()
        {
            var mealPlans = await _context.MealPlans
                .Include(mp => mp.User)
                .Include(mp => mp.MealItems)
                .OrderByDescending(mp => mp.CreatedDate)
                .ToListAsync();

            return View(mealPlans);
        }

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

            // Check if username is already taken
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

        public async Task<IActionResult> ManageRecipes(string searchTerm, string difficulty, string timeFilter, string sortBy, int? categoryId)
        {
            var recipesQuery = _context.Recipes
                .Include(r => r.CreatedBy)
                .Include(r => r.RecipeCategories).ThenInclude(rc => rc.Category)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
                recipesQuery = recipesQuery.Where(r => r.Title.Contains(searchTerm) || r.Description.Contains(searchTerm) || r.Ingredients.Contains(searchTerm));

            if (Enum.TryParse(difficulty, out DifficultyLevel difficultyLevel))
                recipesQuery = recipesQuery.Where(r => r.Difficulty == difficultyLevel);

            if (int.TryParse(timeFilter, out int minutes))
                recipesQuery = recipesQuery.Where(r => (r.PrepTimeMinutes + r.CookTimeMinutes) <= minutes);

            if (categoryId.HasValue)
                recipesQuery = recipesQuery.Where(r => r.RecipeCategories.Any(rc => rc.CategoryId == categoryId));

            recipesQuery = sortBy switch
            {
                "oldest" => recipesQuery.OrderBy(r => r.CreatedDate),
                "fastest" => recipesQuery.OrderBy(r => r.PrepTimeMinutes + r.CookTimeMinutes),
                _ => recipesQuery.OrderByDescending(r => r.CreatedDate)
            };

            ViewBag.Categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
            ViewBag.SelectedCategoryId = categoryId;

            return View(await recipesQuery.ToListAsync());
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