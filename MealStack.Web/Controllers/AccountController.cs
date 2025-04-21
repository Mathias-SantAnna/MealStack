using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MealStack.Infrastructure.Data;
using MealStack.Infrastructure.Data.Entities;
using MealStack.Web.Models;
using System.Threading.Tasks;

namespace MealStack.Web.Controllers
{
    [Authorize]
    public class AccountController : BaseController
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly MealStackDbContext _context;

        public AccountController(UserManager<ApplicationUser> userManager, MealStackDbContext context)
            : base(userManager)
        {
            _userManager = userManager;
            _context = context;
        }

        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }
            
            var model = new ProfileViewModel
            {
                Email = user.Email,
                UserName = user.UserName
            };
            
            return View(model);
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateUsername(ProfileViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }
            
            if (ModelState.IsValid)
            {
                // Check if the username is already taken by someone else
                var existingUser = await _userManager.FindByNameAsync(model.UserName);
                if (existingUser != null && existingUser.Id != user.Id)
                {
                    ModelState.AddModelError("UserName", "This username is already taken.");
                    model.Email = user.Email;
                    return View("Profile", model);
                }
                
                user.UserName = model.UserName;
                user.NormalizedUserName = _userManager.NormalizeName(model.UserName); // Properly normalize the username
                
                var result = await _userManager.UpdateAsync(user);
                
                if (result.Succeeded)
                {
                    TempData["Message"] = "Username updated successfully.";
                    return RedirectToAction(nameof(Profile));
                }
                
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }
            
            // If we got this far, something failed, redisplay form
            model.Email = user.Email;
            return View("Profile", model);
        }
    }
}