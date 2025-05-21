using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MealStack.Infrastructure.Data;
using MealStack.Infrastructure.Data.Entities;
using MealStack.Web.Models;
using MealStack.Web.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MealStack.Web.Controllers
{
    [Authorize]
    public class MealPlanController : BaseController
    {
        private readonly MealStackDbContext _context;
        private readonly IMealPlanService _mealPlanService;
        private readonly ILogger<MealPlanController> _logger;

        public MealPlanController(
            MealStackDbContext context,
            UserManager<ApplicationUser> userManager,
            IMealPlanService mealPlanService,
            ILogger<MealPlanController> logger)
            : base(userManager)
        {
            _context = context;
            _mealPlanService = mealPlanService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var userId = GetUserId();
            var mealPlans = await _mealPlanService.GetUserMealPlansAsync(userId);
            return View(mealPlans);
        }

        public async Task<IActionResult> Details(int id)
        {
            var userId = GetUserId();
            var mealPlan = await _mealPlanService.GetMealPlanByIdAsync(id, userId);
            if (mealPlan == null) return NotFound();
            return View(mealPlan);
        }

        public IActionResult Create()
        {
            var model = new MealPlanViewModel
            {
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddDays(6)
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MealPlanViewModel model)
        {
            model.UserId = GetUserId();
            ModelState.Remove(nameof(model.UserId));

            if (model.EndDate < model.StartDate)
            {
                ModelState.AddModelError(nameof(model.EndDate), "End date cannot be before start date.");
            }

            if (!ModelState.IsValid)
            {
                foreach (var kv in ModelState)
                foreach (var err in kv.Value.Errors)
                    _logger.LogWarning("ModelState error on '{Field}': {Error}", kv.Key, err.ErrorMessage);

                return View(model);
            }

            try
            {
                await _mealPlanService.CreateMealPlanAsync(model);
                TempData["Message"] = "Meal plan created successfully!";
                _logger.LogInformation("User {User} created MealPlan {PlanId}", model.UserId, model.Id);
                return RedirectToAction(nameof(Details), new { id = model.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating MealPlan for user {User}", model.UserId);
                ModelState.AddModelError(string.Empty, "Unexpected error: " + ex.Message);
                return View(model);
            }
        }

        public async Task<IActionResult> Edit(int id)
        {
            var userId = GetUserId();
            var mealPlan = await _mealPlanService.GetMealPlanByIdAsync(id, userId);
            if (mealPlan == null) return NotFound();
            return View(mealPlan);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, MealPlanViewModel model)
        {
            if (id != model.Id)
                return BadRequest("Mismatched ID in route and model.");

            if (model.EndDate < model.StartDate)
                ModelState.AddModelError(nameof(model.EndDate), "End date cannot be before start date.");

            if (!ModelState.IsValid)
            {
                foreach (var kv in ModelState)
                    foreach (var err in kv.Value.Errors)
                        _logger.LogWarning("Edit binding error on '{Field}': {Error}", kv.Key, err.ErrorMessage);
                return View(model);
            }

            try
            {
                model.UserId = GetUserId();
                await _mealPlanService.UpdateMealPlanAsync(model);
                _logger.LogInformation("User {User} updated MealPlan {PlanId}", model.UserId, model.Id);
                TempData["Message"] = "Meal plan updated successfully!";
                return RedirectToAction(nameof(Details), new { id = model.Id });
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Concurrency error editing MealPlan {PlanId}", model.Id);
                ModelState.AddModelError(string.Empty, "This meal plan was updated elsewhere. Please reload and try again.");
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error editing MealPlan {PlanId}", model.Id);
                ModelState.AddModelError(string.Empty, "Unexpected error: " + ex.Message);
                return View(model);
            }
        }

        public async Task<IActionResult> Delete(int id)
        {
            var userId = GetUserId();
            var mealPlan = await _mealPlanService.GetMealPlanByIdAsync(id, userId);
            if (mealPlan == null) return NotFound();
            return View(mealPlan);
        }

        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userId = GetUserId();
            var success = await _mealPlanService.DeleteMealPlanAsync(id, userId);
            if (success)
            {
                _logger.LogInformation("User {User} deleted MealPlan {PlanId}", userId, id);
                TempData["Message"] = "Meal plan deleted successfully.";
            }
            else
            {
                _logger.LogWarning("Failed delete attempt by {User} on MealPlan {PlanId}", userId, id);
                TempData["Error"] = "Could not delete meal plan. It may not exist or you lack permission.";
            }
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> ShoppingList(int id)
        {
            var userId = GetUserId();
            var mealPlan = await _mealPlanService.GetMealPlanByIdAsync(id, userId);
            if (mealPlan == null) return NotFound();
            ViewBag.CheckedCount = mealPlan.ShoppingItems?.Count(i => i.IsChecked) ?? 0;
            ViewBag.UncheckedCount = mealPlan.ShoppingItems?.Count(i => !i.IsChecked) ?? 0;
            return View(mealPlan);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateShoppingList(int id)
        {
            try
            {
                var userId = GetUserId();
                await _mealPlanService.GenerateShoppingListAsync(id, userId);
                _logger.LogInformation("User {User} generated shopping list for MealPlan {PlanId}", userId, id);
                TempData["Message"] = "Shopping list generated successfully!";
                return RedirectToAction(nameof(ShoppingList), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating shopping list for MealPlan {PlanId}", id);
                TempData["Error"] = "Error generating shopping list: " + ex.Message;
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AddShoppingItem(ShoppingListItemViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var errs = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                _logger.LogWarning("AddShoppingItem validation failed: {Errors}", string.Join(", ", errs));
                return Json(new { success = false, errors = errs });
            }

            try
            {
                var userId = GetUserId();
                var created = await _mealPlanService.AddShoppingItemAsync(model, userId);
                _logger.LogInformation("User {User} added ShoppingItem {ItemId} to MealPlan {PlanId}",
                                        userId, created.Id, model.MealPlanId);
                return Json(new { success = true, item = created });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AddShoppingItem for MealPlan {PlanId}", model.MealPlanId);
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateShoppingItem(int itemId, bool isChecked, int mealPlanId)
        {
            try
            {
                var userId = GetUserId();
                await _mealPlanService.UpdateShoppingItemStatusAsync(itemId, isChecked, userId);
                _logger.LogInformation("User {User} set ShoppingItem {ItemId} checked={Checked}", 
                                       userId, itemId, isChecked);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdateShoppingItem {ItemId}", itemId);
                return Json(new { success = false, message = ex.Message });
            }
        }

        public async Task<IActionResult> PrintShoppingList(int id)
        {
            var userId = GetUserId();
            var mealPlan = await _mealPlanService.GetMealPlanByIdAsync(id, userId);
            if (mealPlan == null) return NotFound();

            mealPlan.ShoppingItems = mealPlan.ShoppingItems?
                .Where(i => !i.IsChecked)
                .OrderBy(i => i.Category)
                .ThenBy(i => i.IngredientName)
                .ToList() 
              ?? new List<ShoppingListItemViewModel>();

            return View(mealPlan);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AddMealItem(MealPlanItemViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var errs = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                _logger.LogWarning("AddMealItem validation failed: {Errors}", string.Join(", ", errs));
                return Json(new { success = false, errors = errs });
            }

            try
            {
                model.UserId = GetUserId();
                var added = await _mealPlanService.AddMealItemAsync(model);
                _logger.LogInformation("User {User} added MealItem {ItemId} to MealPlan {PlanId}",
                                       model.UserId, added.Id, model.MealPlanId);
                return Json(new { success = true, item = added });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AddMealItem for MealPlan {PlanId}", model.MealPlanId);
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateMealItem(MealPlanItemViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var errs = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                _logger.LogWarning("UpdateMealItem validation failed: {Errors}", string.Join(", ", errs));
                return Json(new { success = false, errors = errs });
            }

            try
            {
                model.UserId = GetUserId();
                var updated = await _mealPlanService.UpdateMealItemAsync(model);
                _logger.LogInformation("User {User} updated MealItem {ItemId} in MealPlan {PlanId}",
                                       model.UserId, updated.Id, model.MealPlanId);
                return Json(new { success = true, item = updated });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdateMealItem {ItemId}", model.Id);
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveMealItem(int itemId, int mealPlanId)
        {
            try
            {
                var userId = GetUserId();
                await _mealPlanService.RemoveMealItemAsync(itemId, userId);
                _logger.LogInformation("User {User} removed MealItem {ItemId} from MealPlan {PlanId}",
                                        userId, itemId, mealPlanId);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in RemoveMealItem {ItemId}", itemId);
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetRecipes(string term)
        {
            var q = _context.Recipes.AsQueryable();
            if (!string.IsNullOrWhiteSpace(term))
                q = q.Where(r => EF.Functions.ILike(r.Title, $"%{term}%"));

            var results = await q
                .OrderByDescending(r => r.CreatedDate)
                .Select(r => new {
                    id         = r.Id,
                    text       = r.Title,
                    imagePath  = r.ImagePath,
                    servings   = r.Servings
                })
                .Take(20)
                .ToListAsync();

            return Json(results);
        }
    }
}