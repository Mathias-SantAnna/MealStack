using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MealStack.Infrastructure.Data;
using MealStack.Infrastructure.Data.Entities;
using Microsoft.AspNetCore.Hosting;
using System.IO;

namespace MealStack.Web.Controllers
{
    public class CategoryController : BaseController
    {
        private readonly MealStackDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<CategoryController> _logger;

        public CategoryController(
            MealStackDbContext context,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment webHostEnvironment,
            ILogger<CategoryController> logger)
            : base(userManager)
        {
            _context = context;
            _userManager = userManager;
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            var categories = await _context.Categories
                .Include(c => c.RecipeCategories)
                .OrderBy(c => c.Name)
                .ToListAsync();
                
            return View(categories);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            ViewBag.ColorOptions = GetExtendedColorOptions();
            return View(new CategoryEntity());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(CategoryEntity category, IFormFile ImageFile)
        {
            ModelState.Remove("RecipeCategories");
            
            if (ModelState.IsValid)
            {
                try
                {
                    var existingCategory = await _context.Categories
                        .FirstOrDefaultAsync(c => c.Name.ToLower() == category.Name.ToLower());
                    
                    if (existingCategory != null)
                    {
                        ModelState.AddModelError("Name", "A category with this name already exists.");
                        ViewBag.ColorOptions = GetExtendedColorOptions();
                        return View(category);
                    }

                    if (ImageFile != null && ImageFile.Length > 0)
                    {
                        category.ImagePath = await SaveCategoryImage(ImageFile);
                    }
                    
                    _context.Categories.Add(category);
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation("Category created successfully: {CategoryName} with color {ColorClass}", 
                        category.Name, category.ColorClass);
                    
                    TempData["Message"] = "Category created successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating category: {CategoryName}", category.Name);
                    ModelState.AddModelError("", "Error creating category. Please try again.");
                }
            }
            
            ViewBag.ColorOptions = GetExtendedColorOptions();
            return View(category);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return NotFound();
            }

            ViewBag.ColorOptions = GetExtendedColorOptions();
            return View(category);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, CategoryEntity category, IFormFile ImageFile)
        {
            if (id != category.Id)
            {
                _logger.LogWarning("ID mismatch in Edit: URL ID {UrlId} vs Model ID {ModelId}", id, category.Id);
                return NotFound();
            }

            try
            {
                var existingCategory = await _context.Categories.FindAsync(id);
                
                if (existingCategory == null)
                {
                    _logger.LogWarning("Category not found for edit: {CategoryId}", id);
                    return NotFound();
                }

                _logger.LogInformation("Starting edit for category {CategoryId}: {CategoryName}", id, existingCategory.Name);
                _logger.LogInformation("Current ColorClass: {CurrentColor}, New ColorClass: {NewColor}", 
                    existingCategory.ColorClass, category.ColorClass);

                var duplicateCategory = await _context.Categories
                    .FirstOrDefaultAsync(c => c.Name.ToLower() == category.Name.ToLower() && c.Id != id);
                
                if (duplicateCategory != null)
                {
                    ModelState.AddModelError("Name", "A category with this name already exists.");
                    ViewBag.ColorOptions = GetExtendedColorOptions();
                    return View(category);
                }

                if (ImageFile != null && ImageFile.Length > 0)
                {
                    if (!string.IsNullOrEmpty(existingCategory.ImagePath))
                    {
                        DeleteCategoryImage(existingCategory.ImagePath);
                    }
                    
                    existingCategory.ImagePath = await SaveCategoryImage(ImageFile);
                    _logger.LogInformation("Updated image for category {CategoryId}", id);
                }
                
                existingCategory.Name = category.Name;
                existingCategory.Description = category.Description;
                existingCategory.ColorClass = category.ColorClass;
                
                _logger.LogInformation("About to save changes for category {CategoryId}. New ColorClass: {ColorClass}", 
                    id, existingCategory.ColorClass);
                
                _context.Entry(existingCategory).State = EntityState.Modified;
                var changeCount = await _context.SaveChangesAsync();
                
                _logger.LogInformation("Successfully updated category {CategoryId}. Changes saved: {ChangeCount}", 
                    id, changeCount);
                _logger.LogInformation("Final ColorClass after save: {ColorClass}", existingCategory.ColorClass);
                
                TempData["Message"] = $"Category '{existingCategory.Name}' updated successfully with color '{existingCategory.ColorClass}'!";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Concurrency error updating category {CategoryId}", id);
                if (!CategoryExists(category.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating category {CategoryId}: {ErrorMessage}", id, ex.Message);
                TempData["Error"] = "Error updating category. Please try again.";
            }
            
            ViewBag.ColorOptions = GetExtendedColorOptions();
            return View(category);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _context.Categories
                .Include(c => c.RecipeCategories)
                .FirstOrDefaultAsync(c => c.Id == id);
            
            if (category == null)
            {
                return NotFound();
            }

            ViewBag.RecipeCount = category.RecipeCategories.Count;
            return View(category);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var category = await _context.Categories
                    .Include(c => c.RecipeCategories)
                    .FirstOrDefaultAsync(c => c.Id == id);
                
                if (category == null)
                {
                    return NotFound();
                }

                if (category.RecipeCategories.Any())
                {
                    TempData["Error"] = $"Cannot delete category '{category.Name}' because it is being used by {category.RecipeCategories.Count} recipe(s).";
                    return RedirectToAction(nameof(Index));
                }

                if (!string.IsNullOrEmpty(category.ImagePath))
                {
                    DeleteCategoryImage(category.ImagePath);
                }

                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Category deleted successfully: {CategoryName}", category.Name);
                
                TempData["Message"] = "Category deleted successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting category {CategoryId}", id);
                TempData["Error"] = "Error deleting category. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

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
                    colorClass = c.ColorClass ?? "secondary",
                    imagePath = c.ImagePath
                })
                .ToListAsync();
                
            return Json(categories);
        }

        private bool CategoryExists(int id)
        {
            return _context.Categories.Any(e => e.Id == id);
        }

        private async Task<string> SaveCategoryImage(IFormFile imageFile)
        {
            try
            {
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
                var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "categories");
                
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }
                
                var filePath = Path.Combine(uploadsFolder, fileName);
                
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }
                
                return "/images/categories/" + fileName;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving category image");
                throw;
            }
        }

        private void DeleteCategoryImage(string imagePath)
        {
            try
            {
                var fullPath = Path.Combine(_webHostEnvironment.WebRootPath, 
                    imagePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                if (System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting category image: {ImagePath}", imagePath);
            }
        }

        private Dictionary<string, string> GetExtendedColorOptions()
        {
            return new Dictionary<string, string>
            {
                { "primary", "Primary Orange" },
                { "secondary", "Secondary Brown" },
                { "success", "Success Green" },
                { "danger", "Danger Red" },
                { "warning", "Warning Yellow" },
                { "info", "Info Cyan" },
                { "light", "Light Gray" },
                { "dark", "Dark Gray" },
                
                { "purple", "Purple" },
                { "pink", "Pink" },
                { "orange", "Orange" },
                { "teal", "Teal" },
                { "indigo", "Indigo" },
                { "cyan", "Cyan" },
                
                { "lime", "Lime Green" },
                { "amber", "Amber" },
                { "rose", "Rose" },
                { "emerald", "Emerald" },
                { "violet", "Violet" },
                { "sky", "Sky Blue" }
            };
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DebugCategory(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return Json(new { error = "Category not found" });
            }

            return Json(new
            {
                id = category.Id,
                name = category.Name,
                colorClass = category.ColorClass,
                description = category.Description,
                imagePath = category.ImagePath,
                timestamp = DateTime.Now
            });
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> QuickUpdateColor(int id, string colorClass)
        {
            try
            {
                var category = await _context.Categories.FindAsync(id);
                if (category == null)
                {
                    return Json(new { success = false, message = "Category not found" });
                }

                _logger.LogInformation("Quick update: Category {CategoryId} from {OldColor} to {NewColor}", 
                    id, category.ColorClass, colorClass);

                category.ColorClass = colorClass;
        
                _context.Entry(category).State = EntityState.Modified;
                var changes = await _context.SaveChangesAsync();

                _logger.LogInformation("Quick update completed. Changes saved: {ChangeCount}", changes);

                var updatedCategory = await _context.Categories.FindAsync(id);
        
                return Json(new
                {
                    success = true,
                    message = $"Color updated to {colorClass}",
                    categoryId = id,
                    oldColor = category.ColorClass,
                    newColor = updatedCategory.ColorClass,
                    changesSaved = changes,
                    verified = updatedCategory.ColorClass == colorClass
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in quick color update for category {CategoryId}", id);
                return Json(new { success = false, message = ex.Message });
            }
        }
        
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> TestColorUpdate(int id, string color)
        {
            try
            {
                var category = await _context.Categories.FindAsync(id);
                if (category == null)
                {
                    return Json(new { error = "Category not found" });
                }

                var oldColor = category.ColorClass;
                category.ColorClass = color;
        
                _context.Entry(category).State = EntityState.Modified;
                var changes = await _context.SaveChangesAsync();

                await _context.Entry(category).ReloadAsync();

                return Json(new
                {
                    success = true,
                    categoryId = id,
                    oldColor = oldColor,
                    newColor = category.ColorClass,
                    changesSaved = changes,
                    verified = category.ColorClass == color
                });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }
    }
}