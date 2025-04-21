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