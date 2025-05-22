using System;
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
                EndDate   = DateTime.Today.AddDays(6)
            };
            return View(model);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MealPlanViewModel model)
        {
            model.UserId = GetUserId();
            ModelState.Remove(nameof(model.UserId));

            if (model.EndDate < model.StartDate)
                ModelState.AddModelError(nameof(model.EndDate), "End date cannot be before start date.");

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
            var userId   = GetUserId();
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
            var userId   = GetUserId();
            var mealPlan = await _mealPlanService.GetMealPlanByIdAsync(id, userId);
            if (mealPlan == null) return NotFound();
            return View(mealPlan);
        }

        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userId = GetUserId();
            try
            {
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
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting MealPlan {PlanId} for user {User}", id, userId);
                TempData["Error"] = "An error occurred while deleting the meal plan. Please try again.";
            }
            return RedirectToAction(nameof(Index));
        }
        
        public async Task<IActionResult> ShoppingList(int id)
        {
            var userId   = GetUserId();
            var mealPlan = await _mealPlanService.GetMealPlanByIdAsync(id, userId);
            if (mealPlan == null) return NotFound();
            ViewBag.CheckedCount   = mealPlan.ShoppingItems?.Count(i => i.IsChecked) ?? 0;
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
            var userId   = GetUserId();
            var mealPlan = await _mealPlanService.GetMealPlanByIdAsync(id, userId);
            if (mealPlan == null) return NotFound();

            mealPlan.ShoppingItems = mealPlan.ShoppingItems?
                .Where(i => !i.IsChecked)
                .OrderBy(i => i.Category)
                .ThenBy(i => i.IngredientName)
                .ToList()
              ?? new System.Collections.Generic.List<ShoppingListItemViewModel>();

            return View(mealPlan);
        }

        // === AJAX: Add Meal Item ===
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AddMealItem(
            [Bind("MealPlanId,RecipeId,PlannedDate,MealType,Servings,Notes")]
            MealPlanItemViewModel model)
        {
            // Remove validation for fields that will be set programmatically
            ModelState.Remove("UserId");
            ModelState.Remove("RecipeTitle");
            ModelState.Remove("RecipeImagePath");
            
            // FIXED: Make Notes optional by providing default value
            if (string.IsNullOrEmpty(model.Notes))
            {
                model.Notes = string.Empty;
            }

            if (!ModelState.IsValid)
            {
                var errs = ModelState
                    .Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage);
                _logger.LogWarning("AddMealItem validation failed: {Errors}", string.Join(", ", errs));
                return Json(new { success = false, errors = errs });
            }

            try
            {
                model.UserId = GetUserId();

                // Check if same meal already exists to prevent duplicates
                var existingMeal = await _context.MealPlanItems
                    .FirstOrDefaultAsync(mp => 
                        mp.MealPlanId == model.MealPlanId && 
                        mp.RecipeId == model.RecipeId && 
                        mp.PlannedDate.Date == model.PlannedDate.Date && 
                        mp.MealType == model.MealType);

                if (existingMeal != null)
                {
                    _logger.LogWarning(
                        "Duplicate meal detected: User {User} attempted to add duplicate MealItem to MealPlan {PlanId}",
                        model.UserId, model.MealPlanId
                    );
                    return Json(new { success = false, message = "This meal is already on your plan for this date." });
                }

                var recipe = await _context.Recipes
                    .AsNoTracking()
                    .Where(r => r.Id == model.RecipeId)
                    .Select(r => new { r.Title, r.ImagePath })
                    .FirstOrDefaultAsync();

                if (recipe == null)
                    return Json(new { success = false, message = "Selected recipe not found." });

                model.RecipeTitle = recipe.Title;
                model.RecipeImagePath = recipe.ImagePath;

                var added = await _mealPlanService.AddMealItemAsync(model);
                _logger.LogInformation(
                    "User {User} added MealItem {ItemId} to MealPlan {PlanId}",
                    model.UserId, added.Id, model.MealPlanId
                );

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AddMealItem for MealPlan {PlanId}", model.MealPlanId);
                return Json(new { success = false, message = ex.Message });
            }
        }

        // === AJAX: Update Meal Item ===
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateMealItem(
            [Bind("Id,MealPlanId,RecipeId,PlannedDate,MealType,Servings,Notes")]
            MealPlanItemViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var errs = ModelState
                    .Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage);
                _logger.LogWarning("UpdateMealItem validation failed: {Errors}", string.Join(", ", errs));
                return Json(new { success = false, errors = errs });
            }

            try
            {
                model.UserId = GetUserId();

                var recipe = await _context.Recipes
                    .AsNoTracking()
                    .Where(r => r.Id == model.RecipeId)
                    .Select(r => new { r.Title, r.ImagePath })
                    .FirstOrDefaultAsync();

                if (recipe == null)
                    return Json(new { success = false, message = "Selected recipe not found." });

                model.RecipeTitle     = recipe.Title;
                model.RecipeImagePath = recipe.ImagePath;

                await _mealPlanService.UpdateMealItemAsync(model);
                _logger.LogInformation(
                    "User {User} updated MealItem {ItemId} in MealPlan {PlanId}",
                    model.UserId, model.Id, model.MealPlanId
                );

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdateMealItem {ItemId}", model.Id);
                return Json(new { success = false, message = ex.Message });
            }
        }

        // === AJAX: Remove Meal Item ===
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveMealItem(int itemId, int mealPlanId)
        {
            try
            {
                var userId = GetUserId();
                await _mealPlanService.RemoveMealItemAsync(itemId, userId);
                _logger.LogInformation(
                    "User {User} removed MealItem {ItemId} from MealPlan {PlanId}",
                    userId, itemId, mealPlanId
                );
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in RemoveMealItem {ItemId}", itemId);
                return Json(new { success = false, message = ex.Message });
            }
        }
        
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkUpdateShoppingItems(int mealPlanId, string action, int[] itemIds = null)
        {
            try
            {
                var userId = GetUserId();
                
                // Verify user has access to this meal plan
                if (!await _mealPlanService.UserHasAccessToMealPlanAsync(mealPlanId, userId))
                {
                    return Json(new { success = false, message = "Access denied" });
                }

                switch (action.ToLower())
                {
                    case "clearall":
                        await ClearAllShoppingItems(mealPlanId, userId);
                        break;
                    case "checkall":
                        await CheckAllShoppingItems(mealPlanId, userId);
                        break;
                    case "uncheckall":
                        await UncheckAllShoppingItems(mealPlanId, userId);
                        break;
                    case "deleteselected":
                        if (itemIds != null && itemIds.Length > 0)
                            await DeleteSelectedShoppingItems(itemIds, userId);
                        break;
                    default:
                        return Json(new { success = false, message = "Invalid action" });
                }

                _logger.LogInformation("User {User} performed bulk action '{Action}' on MealPlan {PlanId}", 
                                       userId, action, mealPlanId);

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in bulk shopping operation for MealPlan {PlanId}", mealPlanId);
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AddMultipleShoppingItems(int mealPlanId, string itemsText)
        {
            try
            {
                var userId = GetUserId();
                
                if (!await _mealPlanService.UserHasAccessToMealPlanAsync(mealPlanId, userId))
                {
                    return Json(new { success = false, message = "Access denied" });
                }

                var items = ParseMultipleItems(itemsText);
                var addedItems = new List<ShoppingListItemViewModel>();

                foreach (var item in items)
                {
                    item.MealPlanId = mealPlanId;
                    var addedItem = await _mealPlanService.AddShoppingItemAsync(item, userId);
                    addedItems.Add(addedItem);
                }

                _logger.LogInformation("User {User} added {Count} items to MealPlan {PlanId}", 
                                       userId, addedItems.Count, mealPlanId);

                return Json(new { success = true, addedCount = addedItems.Count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding multiple shopping items to MealPlan {PlanId}", mealPlanId);
                return Json(new { success = false, message = ex.Message });
            }
        }

        private async Task ClearAllShoppingItems(int mealPlanId, string userId)
        {
            var checkedItems = await _context.ShoppingListItems
                .Include(s => s.MealPlan)
                .Where(s => s.MealPlanId == mealPlanId && s.MealPlan.UserId == userId && s.IsChecked)
                .ToListAsync();

            _context.ShoppingListItems.RemoveRange(checkedItems);
            await _context.SaveChangesAsync();
        }

        private async Task CheckAllShoppingItems(int mealPlanId, string userId)
        {
            var uncheckedItems = await _context.ShoppingListItems
                .Include(s => s.MealPlan)
                .Where(s => s.MealPlanId == mealPlanId && s.MealPlan.UserId == userId && !s.IsChecked)
                .ToListAsync();

            foreach (var item in uncheckedItems)
            {
                item.IsChecked = true;
            }

            await _context.SaveChangesAsync();
        }

        private async Task UncheckAllShoppingItems(int mealPlanId, string userId)
        {
            var checkedItems = await _context.ShoppingListItems
                .Include(s => s.MealPlan)
                .Where(s => s.MealPlanId == mealPlanId && s.MealPlan.UserId == userId && s.IsChecked)
                .ToListAsync();

            foreach (var item in checkedItems)
            {
                item.IsChecked = false;
            }

            await _context.SaveChangesAsync();
        }

        private async Task DeleteSelectedShoppingItems(int[] itemIds, string userId)
        {
            var items = await _context.ShoppingListItems
                .Include(s => s.MealPlan)
                .Where(s => itemIds.Contains(s.Id) && s.MealPlan.UserId == userId)
                .ToListAsync();

            _context.ShoppingListItems.RemoveRange(items);
            await _context.SaveChangesAsync();
        }

        private List<ShoppingListItemViewModel> ParseMultipleItems(string itemsText)
        {
            var items = new List<ShoppingListItemViewModel>();
            
            if (string.IsNullOrWhiteSpace(itemsText))
                return items;

            var lines = itemsText.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                if (string.IsNullOrEmpty(trimmedLine))
                    continue;

                var parts = trimmedLine.Split(' ', 3);
                var item = new ShoppingListItemViewModel();

                if (parts.Length >= 2 && decimal.TryParse(parts[0], out _))
                {
                    // Format: "2 cups flour" or "1 kg chicken"
                    item.Quantity = parts[0];
                    if (parts.Length == 3)
                    {
                        item.Unit = parts[1];
                        item.IngredientName = parts[2];
                    }
                    else
                    {
                        item.IngredientName = parts[1];
                    }
                }
                else
                {
                    // Format: "milk" or "olive oil"
                    item.IngredientName = trimmedLine;
                }

                item.Category = DetermineItemCategory(item.IngredientName);
                items.Add(item);
            }

            return items;
        }

        private string DetermineItemCategory(string itemName)
        {
            var name = itemName.ToLowerInvariant();
            
            if (new[] { "flour", "sugar", "rice", "pasta", "oats", "cereal", "bread" }.Any(x => name.Contains(x)))
                return "Pantry";
            if (new[] { "chicken", "beef", "pork", "fish", "meat", "seafood", "bacon" }.Any(x => name.Contains(x)))
                return "Meat & Seafood";
            if (new[] { "milk", "cheese", "yogurt", "cream", "butter", "egg" }.Any(x => name.Contains(x)))
                return "Dairy & Eggs";
            if (new[] { "apple", "banana", "orange", "berry", "fruit", "lemon", "lime" }.Any(x => name.Contains(x)))
                return "Fruits";
            if (new[] { "carrot", "potato", "onion", "garlic", "vegetable", "tomato", "lettuce", "pepper", "spinach" }.Any(x => name.Contains(x)))
                return "Vegetables";
            if (new[] { "salt", "pepper", "spice", "herb", "basil", "oregano", "thyme", "cilantro" }.Any(x => name.Contains(x)))
                return "Spices & Herbs";
            if (new[] { "oil", "vinegar", "sauce", "ketchup", "mustard", "mayonnaise" }.Any(x => name.Contains(x)))
                return "Condiments & Oils";
            
            return "Other";
        }

        // === AJAX: Recipe lookup for Select2 ===
        [HttpGet]
        public async Task<IActionResult> GetRecipes(string term)
        {
            var q = _context.Recipes.AsQueryable();
            if (!string.IsNullOrWhiteSpace(term))
                q = q.Where(r => EF.Functions.ILike(r.Title, $"%{term}%"));

            var results = await q
                .OrderByDescending(r => r.CreatedDate)
                .Select(r => new
                {
                    id   = r.Id,
                    text = r.Title
                })
                .Take(20)
                .ToListAsync();

            return Json(results);
        }
    }
}
