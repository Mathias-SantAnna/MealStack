using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MealStack.Infrastructure.Data;
using MealStack.Infrastructure.Data.Entities;

namespace MealStack.Web.Controllers
{
    public class IngredientController : BaseController
    {
        private readonly MealStackDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public IngredientController(
            MealStackDbContext context, 
            UserManager<ApplicationUser> userManager) 
            : base(userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Ingredient
        public async Task<IActionResult> Index(int page = 1)
        {
            int pageSize = 12;
    
            var totalIngredients = await _context.Ingredients.CountAsync();
    
            var ingredients = await _context.Ingredients
                .Include(i => i.CreatedBy)
                .OrderBy(i => i.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        
            var totalPages = (int)Math.Ceiling(totalIngredients / (double)pageSize);
    
            var allCategories = await _context.Ingredients
                .Where(i => !string.IsNullOrEmpty(i.Category))
                .Select(i => i.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();
        
            var allMeasurements = await _context.Ingredients
                .Where(i => !string.IsNullOrEmpty(i.Measurement))
                .Select(i => i.Measurement)
                .Distinct()
                .OrderBy(m => m)
                .ToListAsync();
    
            ViewBag.Categories = allCategories;
            ViewBag.Measurements = allMeasurements;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalIngredients = totalIngredients;
    
            return View(ingredients);
        }

        // API endpoint - Ingredients autocomplete
        [HttpGet]
        public async Task<IActionResult> GetAllIngredients()
        {
            var ingredients = await _context.Ingredients
                .OrderBy(i => i.Name)
                .Select(i => new 
                {
                    id = i.Id,
                    name = i.Name,
                    category = i.Category,
                    measurement = i.Measurement,
                    description = i.Description
                })
                .ToListAsync();
                
            return Json(ingredients);
        }

        // API endpoint - Search ingredients
        [HttpGet]
        public async Task<IActionResult> SearchIngredients(string term)
        {
            if (string.IsNullOrEmpty(term))
            {
                return Json(new List<object>());
            }
            
            var ingredients = await _context.Ingredients
                .Where(i => i.Name.Contains(term))
                .OrderBy(i => i.Name)
                .Select(i => new 
                {
                    id = i.Id,
                    name = i.Name,
                    category = i.Category,
                    measurement = i.Measurement,
                    description = i.Description
                })
                .Take(10)
                .ToListAsync();
                
            return Json(ingredients);
        }

        // API endpoint - Get ingredient categories for dropdown (FIXED - removed duplicate)
        [HttpGet]
        public async Task<IActionResult> GetIngredientCategories()
        {
            try
            {
                // Get categories from existing ingredients
                var categories = await _context.Ingredients
                    .Where(i => !string.IsNullOrEmpty(i.Category))
                    .Select(i => i.Category)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToListAsync();

                var standardCategories = new List<string>
                {
                    "Dairy & Eggs", "Meat & Seafood", "Vegetables", "Fruits",
                    "Grains & Cereals", "Spices & Herbs", "Condiments & Oils",
                    "Pantry Staples", "Beverages", "Baking", "Frozen", "Other"
                };

                var allCategories = categories.Union(standardCategories)
                    .OrderBy(c => c)
                    .ToList();

                return Json(new { success = true, categories = allCategories });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error loading categories: " + ex.Message });
            }
        }

        // Admin-only method to create new category
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateCategory([FromBody] CategoryCreateRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Name))
                {
                    return Json(new { success = false, message = "Category name is required" });
                }

                var existingCategory = await _context.Ingredients
                    .AnyAsync(i => i.Category.ToLower() == request.Name.ToLower());

                if (existingCategory)
                {
                    return Json(new { success = false, message = "Category already exists" });
                }
                
                return Json(new { 
                    success = true, 
                    message = "Category will be available once an ingredient uses it",
                    category = new { name = request.Name.Trim() }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error creating category: {ex.Message}" });
            }
        }

        // API endpoint - New ingredient via AJAX
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> AddIngredientAjax([FromBody] IngredientEntity ingredient)
        {
            if (ingredient == null || string.IsNullOrEmpty(ingredient.Name))
            {
                return BadRequest(new { success = false, message = "Ingredient name is required" });
            }
    
            try
            {
                bool duplicateExists = await _context.Ingredients
                    .AnyAsync(i => i.Name.ToLower() == ingredient.Name.ToLower());
            
                if (duplicateExists)
                {
                    return BadRequest(new { success = false, message = "An ingredient with this name already exists" });
                }
        
                ingredient.CreatedById = _userManager.GetUserId(User) ?? string.Empty;
                ingredient.CreatedDate = DateTime.UtcNow;
        
                _context.Ingredients.Add(ingredient);
                await _context.SaveChangesAsync();
        
                return Json(new 
                {
                    success = true,
                    ingredient = new 
                    {
                        id = ingredient.Id,
                        name = ingredient.Name,
                        category = ingredient.Category ?? string.Empty,
                        measurement = ingredient.Measurement ?? string.Empty,
                        description = ingredient.Description ?? string.Empty
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = $"Error creating ingredient: {ex.Message}" });
            }
        }

        // GET: Ingredient/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var ingredient = await _context.Ingredients.FindAsync(id);
            if (ingredient == null)
            {
                return NotFound();
            }

            return View(ingredient);
        }

        // GET: Ingredient/Create
        [Authorize]
        public IActionResult Create()
        {
            return View(new IngredientEntity());
        }

        // POST: Ingredient/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Create([Bind("Name,Description,Category,Measurement")] IngredientEntity ingredient)
        {
            return await TryExecuteAsync(async () =>
            {
                if (ModelState.IsValid)
                {
                    bool duplicateExists = await _context.Ingredients
                        .AnyAsync(i => i.Name.ToLower() == ingredient.Name.ToLower());
                
                    if (duplicateExists)
                    {
                        ModelState.AddModelError("Name", "An ingredient with this name already exists.");
                        return View(ingredient);
                    }
                    
                    if (User.Identity.IsAuthenticated)
                    {
                        ingredient.CreatedById = _userManager.GetUserId(User);
                    }
            
                    ingredient.CreatedDate = DateTime.UtcNow;
                    _context.Add(ingredient);
                    await _context.SaveChangesAsync();
            
                    TempData["Message"] = "Ingredient created successfully!";
                    return RedirectToAction(nameof(Index));
                }
                return View(ingredient);
            }, "Error creating ingredient. Please try again later.");
        }

        // GET: Ingredient/Edit/5
        [Authorize]
        public async Task<IActionResult> Edit(int id)
        {
            var ingredient = await _context.Ingredients.FindAsync(id);
            if (ingredient == null)
            {
                return NotFound();
            }
            
            if (User.Identity.IsAuthenticated && 
               (ingredient.CreatedById == _userManager.GetUserId(User) || User.IsInRole("Admin")))
            {
                return View(ingredient);
            }
            
            return Forbid();
        }

        // POST: Ingredient/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,Category,Measurement")] IngredientEntity ingredient)
        {
            if (id != ingredient.Id)
            {
                return NotFound();
            }

            var existingIngredient = await _context.Ingredients.FindAsync(id);
            if (existingIngredient == null)
            {
                return NotFound();
            }
            
            if (User.Identity.IsAuthenticated && 
               (existingIngredient.CreatedById == _userManager.GetUserId(User) || User.IsInRole("Admin")))
            {
                if (ModelState.IsValid)
                {
                    existingIngredient.Name = ingredient.Name;
                    existingIngredient.Description = ingredient.Description;
                    existingIngredient.Category = ingredient.Category;
                    existingIngredient.Measurement = ingredient.Measurement;
                    
                    await _context.SaveChangesAsync();
                    TempData["Message"] = "Ingredient updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                return View(ingredient);
            }
            
            return Forbid();
        }

        // GET: Ingredient/Delete/5
        [Authorize]
        public async Task<IActionResult> Delete(int id)
        {
            var ingredient = await _context.Ingredients.FindAsync(id);
            if (ingredient == null)
            {
                return NotFound();
            }
            
            if (User.Identity.IsAuthenticated && 
               (ingredient.CreatedById == _userManager.GetUserId(User) || User.IsInRole("Admin")))
            {
                return View(ingredient);
            }
            
            return Forbid();
        }

        // POST: Ingredient/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var ingredient = await _context.Ingredients.FindAsync(id);
            if (ingredient == null)
            {
                return NotFound();
            }
            
            if (User.Identity.IsAuthenticated && 
               (ingredient.CreatedById == _userManager.GetUserId(User) || User.IsInRole("Admin")))
            {
                _context.Ingredients.Remove(ingredient);
                await _context.SaveChangesAsync();
                TempData["Message"] = "Ingredient deleted successfully!";
                return RedirectToAction(nameof(Index));
            }
            
            return Forbid();
        }
    }

    public class CategoryCreateRequest
    {
        public string Name { get; set; }
        public string Description { get; set; }
    }
}