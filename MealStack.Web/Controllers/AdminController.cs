using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using MealStack.Infrastructure.Data;

namespace MealStack.Web.Controllers
{
    [Authorize]
    public class AdminController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AdminController(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<IActionResult> Index()
        {
            Console.WriteLine($"Current user: {User.Identity?.Name}, Authenticated: {User.Identity?.IsAuthenticated}, Roles: Admin => {User.IsInRole("Admin")}");

            if (!User.IsInRole("Admin"))
            {
                TempData["Error"] = "You do not have permission to access the Admin Panel.";
                return RedirectToAction("Index", "Home");
            }

            var users = _userManager.Users.ToList();
            return View(users);
        }

        public async Task<IActionResult> AssignRole(string userId, string role)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null && await _roleManager.RoleExistsAsync(role))
            {
                await _userManager.AddToRoleAsync(user, role);
                TempData["Message"] = $"Successfully assigned role '{role}' to user.";
            }
            return RedirectToAction("Index");
        }
    }
}