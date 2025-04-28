using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MealStack.Infrastructure.Data;
using MealStack.Infrastructure.Data.Entities;
using System; // Added for DateTime.UtcNow
using System.Linq;
using System.Threading.Tasks;

namespace MealStack.Web.Controllers
{
    [Authorize] // It's generally good practice to authorize the whole controller unless specific actions are public
    public class CategoryController : BaseController
    {
        private readonly MealStackDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CategoryController(
            MealStackDbContext context,
            UserManager<ApplicationUser> userManager)
            : base(userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Category
        [AllowAnonymous] // Allow anyone to view the category list
        public async Task<IActionResult> Index()
        {
            var categories = await _context.Categories
                .Include(c => c.CreatedBy) // Consider if CreatedBy is needed here, maybe not for the public index view
                .OrderBy(c => c.Name)
                .ToListAsync();

            return View(categories);
        }

        // GET: Category/Create
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Category/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(CategoryEntity category)
        {
            // Explicitly check if ColorClass is empty/null and set to null if it's the default/placeholder value
            if (string.IsNullOrEmpty(category.ColorClass) || category.ColorClass == "---") // Adjust "---" if your default option value is different
            {
                category.ColorClass = null; // Store null in DB if no color is selected
            }

            if (ModelState.IsValid)
            {
                // *** FIX HERE: Use _userManager.GetUserId(User) ***
                category.CreatedById = _userManager.GetUserId(User); // Use UserManager to get the current user's ID
                category.CreatedDate = DateTime.UtcNow;

                _context.Add(category);
                await _context.SaveChangesAsync();

                TempData["Message"] = "Category created successfully!";
                return RedirectToAction(nameof(Index));
            }
            // If model state is invalid, return the view with the submitted category data
            // The dropdown will automatically re-select the previously chosen value
            return View(category);
        }

        // GET: Category/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return NotFound();
            }

            // If ColorClass is null in DB, set it to empty string or default value for dropdown consistency if needed
            // category.ColorClass ??= ""; // Or "---" depending on your dropdown setup, or handle in view

            return View(category);
        }

        // POST: Category/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, CategoryEntity category) // The 'category' parameter comes from the form submission
        {
            if (id != category.Id)
            {
                return NotFound();
            }

            // Explicitly check if ColorClass is empty/null and set to null if it's the default/placeholder value
             if (string.IsNullOrEmpty(category.ColorClass) || category.ColorClass == "---") // Adjust "---" if your default option value is different
            {
                category.ColorClass = null; // Store null in DB if no color is selected
            }


            // We need to fetch the existing entity first to avoid issues with tracking and concurrency,
            // and to preserve properties not included in the form (like CreatedById, CreatedDate).
            var existingCategory = await _context.Categories.FindAsync(id);

            if (existingCategory == null)
            {
                 return NotFound();
            }

            // Update only the properties that should be editable from the form
            existingCategory.Name = category.Name;
            existingCategory.Description = category.Description;
            // *** CHANGE HERE: Update ColorClass instead of Color ***
            existingCategory.ColorClass = category.ColorClass;

            // Manually validate the existingCategory *after* updating its properties
            // This ensures validation rules (like [Required]) are checked against the final values
            if (TryValidateModel(existingCategory))
            {
                 try
                {
                    // _context.Update(existingCategory); // Not needed if you modify the tracked entity directly
                    await _context.SaveChangesAsync();

                    TempData["Message"] = "Category updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CategoryExists(category.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                         // Log the exception details
                        ModelState.AddModelError(string.Empty, "The record you attempted to edit "
                            + "was modified by another user after you got the original value. The "
                            + "edit operation was canceled and the current values in the database "
                            + "have been displayed. If you still want to edit this record, click "
                            + "the Save button again. Otherwise click the Back to List hyperlink.");
                        // Optional: Reload category with current DB values if you want to show them
                        // category = await _context.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
                    }
                }
            }

            // If model state is invalid (either initially or after TryValidateModel), return the view
            // Pass the 'existingCategory' back to the view as it contains the latest attempted changes
            // and potentially error messages. The dropdown will bind to existingCategory.ColorClass
             return View(existingCategory);
        }


        // GET: Category/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            // Include related recipes count to inform the admin
             var category = await _context.Categories
                .Include(c => c.RecipeCategories) // Assuming you have navigation property RecipeCategories
                .FirstOrDefaultAsync(m => m.Id == id);

            if (category == null)
            {
                return NotFound();
            }
             // You might want to pass the count to the view: ViewBag.RecipeCount = category.RecipeCategories.Count;

            return View(category);
        }

        // POST: Category/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                 TempData["ErrorMessage"] = "Category not found."; // Use a different key for errors
                return RedirectToAction(nameof(Index));
            }

             // Optional: Add check here if category is associated with recipes and prevent deletion or warn
            // var recipeCount = await _context.RecipeCategories.CountAsync(rc => rc.CategoryId == id);
            // if (recipeCount > 0) {
            //     TempData["ErrorMessage"] = $"Cannot delete category '{category.Name}' as it is associated with {recipeCount} recipe(s).";
            //     return RedirectToAction(nameof(Delete), new { id = id }); // Go back to delete confirmation page
            // }


            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Category deleted successfully!";
            return RedirectToAction(nameof(Index));
        }

        // API endpoint to get all categories
        [HttpGet]
        [AllowAnonymous] // Make API endpoint public if needed, e.g., for AJAX calls from public pages
        public async Task<IActionResult> GetAllCategories()
        {
            var categories = await _context.Categories
                .OrderBy(c => c.Name)
                .Select(c => new
                {
                    id = c.Id,
                    name = c.Name,
                    // *** CHANGE HERE: Select ColorClass instead of Color, and rename the JSON property ***
                    colorClass = c.ColorClass ?? "secondary" // Provide a default class if null
                })
                .ToListAsync();

            return Json(categories);
        }

        private bool CategoryExists(int id)
        {
            // Use AnyAsync for async check
            // return await _context.Categories.AnyAsync(e => e.Id == id);
             // Sticking to synchronous for now as original code was sync
             return _context.Categories.Any(e => e.Id == id);
        }
    }
}