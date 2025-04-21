using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MealStack.Infrastructure.Data;
using MealStack.Infrastructure.Data.Entities;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MealStack.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly MealStackDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AdminController(
            MealStackDbContext context, 
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager)
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

        public async Task<IActionResult> ManageRecipes()
        {
            // Get all recipes with their creators
            var recipes = await _context.Recipes
                .Include(r => r.CreatedBy)
                .OrderByDescending(r => r.CreatedDate)
                .ToListAsync();
            
            return View(recipes);
        }

        public async Task<IActionResult> ManageIngredients()
        {
            // Get all ingredients with their creators
            var ingredients = await _context.Set<IngredientEntity>()
                .Include(i => i.CreatedBy)
                .OrderBy(i => i.Name)
                .ToListAsync();
                
            return View(ingredients);
        }

        public async Task<IActionResult> Categories()
        {
            // Forward to the CategoryController's Index action
            return RedirectToAction("Index", "Category");
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
    }

    public class UserViewModel
    {
        public string Id { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public List<string> Roles { get; set; } = new List<string>();
        public int RecipesCount { get; set; }
        public bool EmailConfirmed { get; set; }
    }
}