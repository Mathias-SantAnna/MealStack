using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using MealStack.Infrastructure.Data;
using System.Threading.Tasks;
using System.Linq;

namespace MealStack.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly MealStackDbContext _context;

        public AdminController(
            UserManager<IdentityUser> userManager, 
            RoleManager<IdentityRole> roleManager,
            MealStackDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        // User Management
        public async Task<IActionResult> Index()
        {
            var users = _userManager.Users.ToList();
            return View(users);
        }

        public async Task<IActionResult> AssignRole(string userId, string role)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null && await _roleManager.RoleExistsAsync(role))
            {
                // Check if user already has this role
                if (await _userManager.IsInRoleAsync(user, role))
                {
                    TempData["Message"] = $"User already has the role '{role}'.";
                }
                else
                {
                    // Add the role
                    var result = await _userManager.AddToRoleAsync(user, role);
                    if (result.Succeeded)
                    {
                        TempData["Message"] = $"Successfully assigned role '{role}' to user.";
                    }
                    else
                    {
                        TempData["Error"] = $"Failed to assign role: {string.Join(", ", result.Errors.Select(e => e.Description))}";
                    }
                }
            }
            return RedirectToAction("Index");
        }

        // Recipe Management
        public IActionResult ManageRecipes()
        {
            return View();
        }

        // Ingredient Management
        public IActionResult ManageIngredients()
        {
            return View();
        }

        // Category Management
        public IActionResult Categories()
        {
            return View();
        }
    }
}