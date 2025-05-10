using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MealStack.Infrastructure.Data;
using MealStack.Infrastructure.Data.Entities;


namespace MealStack.Web.Controllers
{
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

        // GET: Category - View is accessible to all
        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            var categories = await _context.Categories
                .OrderBy(c => c.Name)
                .ToListAsync();
                
            return View(categories);
        }

        // GET: Category/Create - Admin only
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Category/Create - Admin only
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(CategoryEntity category)
        {
            if (ModelState.IsValid)
            {
                
                _context.Add(category);
                await _context.SaveChangesAsync();
                
                TempData["Message"] = "Category created successfully!";
                return RedirectToAction(nameof(Index));
            }
            return View(category);
        }

        // GET: Category/Edit/5 - Admin only
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return NotFound();
            }

            return View(category);
        }

        // POST: Category/Edit/5 - Admin only
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, CategoryEntity category)
        {
            if (id != category.Id)
            {
                return NotFound();
            }

            // Get existing category to preserve creation info
            var existingCategory = await _context.Categories.FindAsync(id);
            
            if (existingCategory == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    existingCategory.Name = category.Name;
                    existingCategory.Description = category.Description;
                    existingCategory.ColorClass = category.ColorClass;
                    
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
                        throw;
                    }
                }
            }
            return View(category);
        }

        // GET: Category/Delete/5 - Admin only
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return NotFound();
            }

            return View(category);
        }

        // POST: Category/Delete/5 - Admin only
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return NotFound();
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();
            
            TempData["Message"] = "Category deleted successfully!";
            return RedirectToAction(nameof(Index));
        }

        // API endpoint to get all categories - accessible to all
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAllCategories()
        {
            var categories = await _context.Categories
                .OrderBy(c => c.Name)
                .Select(c => new
                {
                    id = c.Id,
                    name = c.Name,
                    colorClass = c.ColorClass ?? "secondary"
                })
                .ToListAsync();
                
            return Json(categories);
        }

        private bool CategoryExists(int id)
        {
            return _context.Categories.Any(e => e.Id == id);
        }
    }
}