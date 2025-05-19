using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MealStack.Infrastructure.Data;
using MealStack.Infrastructure.Data.Entities;
using MealStack.Web.Models;
using MealStack.Web.Services;
using MealStack.Web.Services.Interfaces;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace MealStack.Web.Controllers
{
    [Authorize]
    public class MealPlanController : BaseController
    {
        private readonly MealStackDbContext _context;
        private readonly IMealPlanService _mealPlanService;
        private readonly UserManager<ApplicationUser> _userManager;

        public MealPlanController(
            MealStackDbContext context,
            UserManager<ApplicationUser> userManager,
            IMealPlanService mealPlanService) 
            : base(userManager)
        {
            _context = context;
            _mealPlanService = mealPlanService;
            _userManager = userManager;
        }

        // GET: MealPlan
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            var mealPlans = await _mealPlanService.GetUserMealPlansAsync(userId);
            return View(mealPlans);
        }

        // GET: MealPlan/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var userId = _userManager.GetUserId(User);
            var mealPlan = await _mealPlanService.GetMealPlanByIdAsync(id, userId);
            
            if (mealPlan == null)
            {
                return NotFound();
            }

            return View(mealPlan);
        }

        // GET: MealPlan/Create
        public IActionResult Create()
        {
            var model = new MealPlanViewModel
            {
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddDays(6) 
            };
            
            return View(model);
        }

        // POST: MealPlan/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MealPlanViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    model.UserId = _userManager.GetUserId(User);
                    await _mealPlanService.CreateMealPlanAsync(model);
                    TempData["Message"] = "Meal plan created successfully!";
                    return RedirectToAction(nameof(Details), new { id = model.Id });
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error creating meal plan: {ex.Message}");
                }
            }
            
            return View(model);
        }

        // GET: MealPlan/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var userId = _userManager.GetUserId(User);
            var mealPlan = await _mealPlanService.GetMealPlanByIdAsync(id, userId);
            
            if (mealPlan == null)
            {
                return NotFound();
            }
            
            return View(mealPlan);
        }

        // POST: MealPlan/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, MealPlanViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    model.UserId = _userManager.GetUserId(User);
                    await _mealPlanService.UpdateMealPlanAsync(model);
                    TempData["Message"] = "Meal plan updated successfully!";
                    return RedirectToAction(nameof(Details), new { id = model.Id });
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error updating meal plan: {ex.Message}");
                }
            }
            
            return View(model);
        }

        // GET: MealPlan/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var userId = _userManager.GetUserId(User);
            var mealPlan = await _mealPlanService.GetMealPlanByIdAsync(id, userId);
            
            if (mealPlan == null)
            {
                return NotFound();
            }
            
            return View(mealPlan);
        }

        // POST: MealPlan/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userId = _userManager.GetUserId(User);
            var result = await _mealPlanService.DeleteMealPlanAsync(id, userId);
            
            if (result)
            {
                TempData["Message"] = "Meal plan deleted successfully.";
                return RedirectToAction(nameof(Index));
            }
            else
            {
                TempData["Error"] = "Could not delete meal plan. It may have been already deleted or you don't have permission.";
                return RedirectToAction(nameof(Index));
            }
        }
        
        // GET: MealPlan/ShoppingList/5
        public async Task<IActionResult> ShoppingList(int id)
        {
            var userId = _userManager.GetUserId(User);
            var mealPlan = await _mealPlanService.GetMealPlanByIdAsync(id, userId);
            
            if (mealPlan == null)
            {
                return NotFound();
            }
            
            return View(mealPlan);
        }
        
        // POST: MealPlan/GenerateShoppingList/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateShoppingList(int id)
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                await _mealPlanService.GenerateShoppingListAsync(id, userId);
                
                TempData["Message"] = "Shopping list generated successfully!";
                return RedirectToAction(nameof(ShoppingList), new { id });
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error generating shopping list: {ex.Message}";
                return RedirectToAction(nameof(Details), new { id });
            }
        }
        
        // POST: MealPlan/UpdateShoppingItem
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateShoppingItem(int itemId, bool isChecked, int mealPlanId)
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                await _mealPlanService.UpdateShoppingItemStatusAsync(itemId, isChecked, userId);
                
                return Json(new { success = true });
            }
            catch (Exception)
            {
                return Json(new { success = false });
            }
        }
        
        // POST: MealPlan/AddMealItem
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddMealItem(MealPlanItemViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    model.UserId = _userManager.GetUserId(User);

                    var result = await _mealPlanService.AddMealItemAsync(model);
                    return Json(new { success = true, item = result });
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = ex.Message });
                }
            }
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
            return Json(new { success = false, errors = errors });
        }
        
        // POST: MealPlan/RemoveMealItem
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveMealItem(int itemId, int mealPlanId)
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                await _mealPlanService.RemoveMealItemAsync(itemId, userId);
                
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        
        // GET: MealPlan/PrintShoppingList/5
        public async Task<IActionResult> PrintShoppingList(int id)
        {
            var userId = _userManager.GetUserId(User);
            var mealPlan = await _mealPlanService.GetMealPlanByIdAsync(id, userId);
            
            if (mealPlan == null)
            {
                return NotFound();
            }
            
            return View(mealPlan);
        }
        
        // Ajax endpoint to get recipes for selection
        [HttpGet]
        public async Task<IActionResult> GetRecipes(string term)
        {
            var userId = _userManager.GetUserId(User);
            
            var query = _context.Recipes.AsQueryable();
            
            if (!string.IsNullOrEmpty(term))
            {
                query = query.Where(r => r.Title.Contains(term));
            }
            
            var recipes = await query
                .OrderByDescending(r => r.CreatedDate)
                .Select(r => new
                {
                    id = r.Id,
                    text = r.Title,
                    imagePath = r.ImagePath,
                    servings = r.Servings
                })
                .Take(20)
                .ToListAsync();
                
            return Json(recipes);
        }
    }
}