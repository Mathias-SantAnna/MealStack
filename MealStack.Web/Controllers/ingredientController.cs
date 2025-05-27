using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MealStack.Infrastructure.Data;
using MealStack.Infrastructure.Data.Entities;
using System.Text.Json;

namespace MealStack.Web.Controllers
{
    public class IngredientController : BaseController
    {
        #region Private Fields
        private readonly MealStackDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<IngredientController> _logger;
        #endregion

        #region Constructor
        public IngredientController(
            MealStackDbContext context, 
            UserManager<ApplicationUser> userManager,
            ILogger<IngredientController> logger) 
            : base(userManager)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }
        #endregion

        #region Public Actions - CRUD Operations
        
        public async Task<IActionResult> Index(
            string searchTerm, 
            string category, 
            string measurement, 
            string createdBy, 
            string hasDescription, 
            string sortBy = "name", 
            int page = 1)
        {
            return await TryExecuteAsync(async () =>
            {
                int pageSize = 12;
                
                var ingredientsQuery = _context.Ingredients
                    .Include(i => i.CreatedBy)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    var searchPattern = $"%{searchTerm}%";
                    ingredientsQuery = ingredientsQuery.Where(i => 
                        EF.Functions.Like(i.Name, searchPattern) || 
                        (i.Description != null && EF.Functions.Like(i.Description, searchPattern)) || 
                        (i.Category != null && EF.Functions.Like(i.Category, searchPattern)));
                }

                if (!string.IsNullOrEmpty(category))
                {
                    ingredientsQuery = ingredientsQuery.Where(i => i.Category == category);
                }

                if (!string.IsNullOrEmpty(measurement))
                {
                    ingredientsQuery = ingredientsQuery.Where(i => i.Measurement == measurement);
                }

                if (!string.IsNullOrEmpty(createdBy))
                {
                    ingredientsQuery = ingredientsQuery.Where(i => i.CreatedById == createdBy);
                }

                if (!string.IsNullOrEmpty(hasDescription))
                {
                    if (bool.TryParse(hasDescription, out bool hasDesc))
                    {
                        if (hasDesc)
                            ingredientsQuery = ingredientsQuery.Where(i => !string.IsNullOrEmpty(i.Description));
                        else
                            ingredientsQuery = ingredientsQuery.Where(i => string.IsNullOrEmpty(i.Description));
                    }
                }

                ingredientsQuery = sortBy switch
                {
                    "name_desc" => ingredientsQuery.OrderByDescending(i => i.Name),
                    "category" => ingredientsQuery.OrderBy(i => i.Category).ThenBy(i => i.Name),
                    "measurement" => ingredientsQuery.OrderBy(i => i.Measurement).ThenBy(i => i.Name),
                    "newest" => ingredientsQuery.OrderByDescending(i => i.CreatedDate),
                    "author" => ingredientsQuery.OrderBy(i => i.CreatedBy.UserName).ThenBy(i => i.Name),
                    _ => ingredientsQuery.OrderBy(i => i.Name)
                };

                var totalIngredients = await ingredientsQuery.CountAsync();

                var ingredients = await ingredientsQuery
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = (int)Math.Ceiling(totalIngredients / (double)pageSize);
                ViewBag.TotalItems = totalIngredients;
                ViewBag.TotalIngredients = totalIngredients;
                
                ViewData["SearchTerm"] = searchTerm;
                ViewData["Category"] = category;
                ViewData["Measurement"] = measurement;
                ViewData["CreatedBy"] = createdBy;
                ViewData["HasDescription"] = hasDescription;
                ViewData["SortBy"] = sortBy;

                ViewBag.Categories = await _context.Ingredients
                    .Where(i => !string.IsNullOrEmpty(i.Category))
                    .Select(i => i.Category)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToListAsync();

                ViewBag.Measurements = await _context.Ingredients
                    .Where(i => !string.IsNullOrEmpty(i.Measurement))
                    .Select(i => i.Measurement)
                    .Distinct()
                    .OrderBy(m => m)
                    .ToListAsync();

                ViewBag.Authors = await _context.Ingredients
                    .Where(i => i.CreatedBy != null)
                    .Select(i => new { Id = i.CreatedById, Name = i.CreatedBy.UserName })
                    .Distinct()
                    .OrderBy(a => a.Name)
                    .ToListAsync();

                return View(ingredients);
            }, "Error loading ingredients. Please try again later.");
        }
        
        [HttpGet]
        public async Task<IActionResult> SearchIngredients(string term)
        {
            if (string.IsNullOrEmpty(term))
            {
                return Json(new List<object>());
            }

            try
            {
                var searchPattern = $"%{term}%";
                var ingredients = await _context.Ingredients
                    .Where(i => EF.Functions.Like(i.Name, searchPattern))
                    .Select(i => new
                    {
                        id = i.Id,
                        name = i.Name,
                        category = i.Category,
                        measurement = i.Measurement
                    })
                    .Take(10)
                    .ToListAsync();

                return Json(ingredients);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching ingredients for term: {Term}", term);
                return Json(new List<object>());
            }
        }

        #endregion

        #region ADMIN BULK ACTIONS 

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> BulkAction(string action, string ingredientIds, BulkActionOptions options)
        {
            return await TryExecuteAsync(async () =>
            {
                if (string.IsNullOrEmpty(action) || string.IsNullOrEmpty(ingredientIds))
                {
                    return Json(new { success = false, message = "Invalid bulk action parameters" });
                }

                var ingredientIdList = ingredientIds.Split(',').Select(int.Parse).ToList();
                var ingredients = await _context.Ingredients
                    .Where(i => ingredientIdList.Contains(i.Id))
                    .ToListAsync();

                if (!ingredients.Any())
                {
                    return Json(new { success = false, message = "No ingredients found for the selected IDs" });
                }

                try
                {
                    switch (action.ToLower())
                    {
                        case "assigncategory":
                            if (!string.IsNullOrEmpty(options.CategoryName))
                            {
                                foreach (var ingredient in ingredients)
                                    ingredient.Category = options.CategoryName;
                                
                                await _context.SaveChangesAsync();
                                return Json(new { success = true, message = $"Assigned category '{options.CategoryName}' to {ingredients.Count} ingredients" });
                            }
                            return Json(new { success = false, message = "No category specified" });

                        case "changecategory":
                            if (!string.IsNullOrEmpty(options.CategoryName))
                            {
                                foreach (var ingredient in ingredients)
                                    ingredient.Category = options.CategoryName;
                                
                                await _context.SaveChangesAsync();
                                return Json(new { success = true, message = $"Changed category to '{options.CategoryName}' for {ingredients.Count} ingredients" });
                            }
                            return Json(new { success = false, message = "No category specified" });

                        case "clearcategory":
                            foreach (var ingredient in ingredients)
                                ingredient.Category = null;
                            
                            await _context.SaveChangesAsync();
                            return Json(new { success = true, message = $"Cleared category for {ingredients.Count} ingredients" });

                        case "setmeasurement":
                            if (!string.IsNullOrEmpty(options.MeasurementUnit))
                            {
                                foreach (var ingredient in ingredients)
                                    ingredient.Measurement = options.MeasurementUnit;
                                
                                await _context.SaveChangesAsync();
                                return Json(new { success = true, message = $"Set measurement to '{options.MeasurementUnit}' for {ingredients.Count} ingredients" });
                            }
                            return Json(new { success = false, message = "No measurement unit specified" });

                        case "clearmeasurement":
                            foreach (var ingredient in ingredients)
                                ingredient.Measurement = null;
                            
                            await _context.SaveChangesAsync();
                            return Json(new { success = true, message = $"Cleared measurement for {ingredients.Count} ingredients" });

                        case "export":
                            return await HandleExportIngredients(ingredients, options.ExportFormat ?? "csv");

                        case "delete":
                            var safeToDelete = new List<IngredientEntity>();
                            foreach (var ingredient in ingredients)
                            {
                                var isUsed = await _context.Recipes.AnyAsync(r => r.Ingredients.Contains(ingredient.Name));
                                if (!isUsed)
                                    safeToDelete.Add(ingredient);
                            }

                            if (safeToDelete.Count < ingredients.Count)
                            {
                                var usedCount = ingredients.Count - safeToDelete.Count;
                                return Json(new { 
                                    success = false, 
                                    message = $"Cannot delete {usedCount} ingredients that are used in recipes. Only {safeToDelete.Count} ingredients can be safely deleted." 
                                });
                            }

                            _context.Ingredients.RemoveRange(safeToDelete);
                            await _context.SaveChangesAsync();
                            return Json(new { success = true, message = $"Successfully deleted {safeToDelete.Count} ingredients" });

                        default:
                            return Json(new { success = false, message = "Unknown bulk action: " + action });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error executing bulk action {Action} on {Count} ingredients", action, ingredients.Count);
                    return Json(new { success = false, message = $"Error executing bulk action: {ex.Message}" });
                }
            }, "Error executing bulk action. Please try again later.");
        }

        #endregion

        #region Other existing methods...
        // Keep all your existing methods like Details, Create, Edit, Delete, etc.
        // I'm only showing the key fixes above for brevity
        #endregion

        #region Private Helper Methods

        private async Task<IActionResult> HandleExportIngredients(List<IngredientEntity> ingredients, string format)
        {
            switch (format.ToLower())
            {
                case "csv":
                    var csv = GenerateIngredientsCsv(ingredients);
                    return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", $"ingredients_export_{DateTime.Now:yyyyMMdd}.csv");

                case "json":
                    var json = System.Text.Json.JsonSerializer.Serialize(ingredients.Select(i => new
                    {
                        i.Id,
                        i.Name,
                        i.Description,
                        i.Category,
                        i.Measurement,
                        CreatedBy = i.CreatedBy?.UserName,
                        CreatedDate = i.CreatedDate.ToString("yyyy-MM-dd")
                    }), new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                    return File(System.Text.Encoding.UTF8.GetBytes(json), "application/json", $"ingredients_export_{DateTime.Now:yyyyMMdd}.json");

                default:
                    return Json(new { success = false, message = "Unsupported export format" });
            }
        }

        private string GenerateIngredientsCsv(List<IngredientEntity> ingredients)
        {
            var csv = new System.Text.StringBuilder();
            csv.AppendLine("ID,Name,Description,Category,Measurement,CreatedBy,CreatedDate");

            foreach (var ingredient in ingredients)
            {
                csv.AppendLine($"{ingredient.Id}," +
                              $"\"{ingredient.Name}\"," +
                              $"\"{ingredient.Description ?? ""}\"," +
                              $"\"{ingredient.Category ?? ""}\"," +
                              $"\"{ingredient.Measurement ?? ""}\"," +
                              $"\"{ingredient.CreatedBy?.UserName ?? ""}\"," +
                              $"{ingredient.CreatedDate:yyyy-MM-dd}");
            }

            return csv.ToString();
        }
        
        private async Task<bool> CanUserModifyIngredient(string ingredientCreatedById)
        {
            var userId = GetUserId();
            return ingredientCreatedById == userId || User.IsInRole("Admin");
        }

        #endregion

        #region Details
        public async Task<IActionResult> Details(int id)
        {
            return await TryExecuteAsync(async () =>
            {
                var ingredient = await _context.Ingredients
                    .Include(i => i.CreatedBy)
                    .FirstOrDefaultAsync(i => i.Id == id);

                if (ingredient == null)
                {
                    return NotFound();
                }

                return View(ingredient);
            }, "Error loading ingredient details. Please try again later.");
        }
        #endregion
        
        #region Create
        [Authorize]
        public IActionResult Create()
        {
            return View();
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Create(IngredientEntity ingredient)
        {
            return await TryExecuteAsync(async () =>
            {
                ingredient.CreatedById = GetUserId();
                ingredient.CreatedDate = DateTime.UtcNow;

                // Remove validation for auto-set fields
                ModelState.Remove("CreatedById");
                ModelState.Remove("CreatedDate");
                ModelState.Remove("CreatedBy");

                if (ModelState.IsValid)
                {
                    // Check for duplicates (case insensitive) - FIXED
                    var existingIngredient = await _context.Ingredients
                        .FirstOrDefaultAsync(i => EF.Functions.Like(i.Name, ingredient.Name));

                    if (existingIngredient != null)
                    {
                        ModelState.AddModelError("Name", "An ingredient with this name already exists.");
                        return View(ingredient);
                    }

                    _context.Ingredients.Add(ingredient);
                    await _context.SaveChangesAsync();

                    TempData["Message"] = "Ingredient created successfully!";
                    return RedirectToAction(nameof(Index));
                }

                return View(ingredient);
            }, "Error creating ingredient. Please check your input and try again.");
        }
        #endregion

        #region Edit
        [Authorize]
        public async Task<IActionResult> Edit(int id)
        {
            return await TryExecuteAsync(async () =>
            {
                var ingredient = await _context.Ingredients.FindAsync(id);
                
                if (ingredient == null)
                {
                    return NotFound();
                }

                if (!await CanUserModifyIngredient(ingredient.CreatedById))
                {
                    return Forbid();
                }

                return View(ingredient);
            }, "Error loading ingredient for editing. Please try again later.");
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Edit(int id, IngredientEntity ingredient)
        {
            return await TryExecuteAsync(async () =>
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

                if (!await CanUserModifyIngredient(existingIngredient.CreatedById))
                {
                    return Forbid();
                }

                // Remove validation for auto-set fields
                ModelState.Remove("CreatedById");
                ModelState.Remove("CreatedDate");
                ModelState.Remove("CreatedBy");

                if (ModelState.IsValid)
                {
                    // Check for duplicates (excluding current ingredient, case insensitive) - FIXED
                    var duplicateIngredient = await _context.Ingredients
                        .FirstOrDefaultAsync(i => EF.Functions.Like(i.Name, ingredient.Name) && i.Id != id);

                    if (duplicateIngredient != null)
                    {
                        ModelState.AddModelError("Name", "An ingredient with this name already exists.");
                        return View(ingredient);
                    }

                    // Update properties
                    existingIngredient.Name = ingredient.Name;
                    existingIngredient.Description = ingredient.Description;
                    existingIngredient.Category = ingredient.Category;
                    existingIngredient.Measurement = ingredient.Measurement;

                    await _context.SaveChangesAsync();

                    TempData["Message"] = "Ingredient updated successfully!";
                    return RedirectToAction(nameof(Index));
                }

                return View(ingredient);
            }, "Error updating ingredient. Please check your input and try again.");
        }
        #endregion

        #region Delete
        [Authorize]
        public async Task<IActionResult> Delete(int id)
        {
            return await TryExecuteAsync(async () =>
            {
                var ingredient = await _context.Ingredients
                    .Include(i => i.CreatedBy)
                    .FirstOrDefaultAsync(i => i.Id == id);

                if (ingredient == null)
                {
                    return NotFound();
                }

                if (!await CanUserModifyIngredient(ingredient.CreatedById))
                {
                    return Forbid();
                }

                return View(ingredient);
            }, "Error loading ingredient for deletion. Please try again later.");
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            return await TryExecuteAsync(async () =>
            {
                var ingredient = await _context.Ingredients.FindAsync(id);
                if (ingredient == null)
                {
                    return NotFound();
                }

                if (!await CanUserModifyIngredient(ingredient.CreatedById))
                {
                    return Forbid();
                }

                _context.Ingredients.Remove(ingredient);
                await _context.SaveChangesAsync();

                TempData["Message"] = "Ingredient deleted successfully!";
                return RedirectToAction(nameof(Index));
            }, "Error deleting ingredient. Please try again later.");
        }
        #endregion

        #region AJAX
        [HttpGet]
        public async Task<IActionResult> GetAllIngredients()
        {
            try
            {
                var ingredients = await _context.Ingredients
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all ingredients");
                return Json(new List<object>());
            }
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> AddIngredientAjax(IngredientCreateModel model)
        {
            try
            {
                if (string.IsNullOrEmpty(model.Name))
                {
                    return Json(new { success = false, message = "Ingredient name is required" });
                }

                // Check for duplicates (case insensitive) - FIXED
                var existingIngredient = await _context.Ingredients
                    .FirstOrDefaultAsync(i => EF.Functions.Like(i.Name, model.Name));

                if (existingIngredient != null)
                {
                    return Json(new { success = false, message = "An ingredient with this name already exists" });
                }

                var ingredient = new IngredientEntity
                {
                    Name = model.Name,
                    Category = model.Category,
                    Measurement = model.Measurement,
                    Description = model.Description,
                    CreatedById = GetUserId(),
                    CreatedDate = DateTime.UtcNow
                };

                _context.Ingredients.Add(ingredient);
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    ingredient = new
                    {
                        id = ingredient.Id,
                        name = ingredient.Name,
                        category = ingredient.Category,
                        measurement = ingredient.Measurement
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating ingredient via AJAX");
                return Json(new { success = false, message = "Error creating ingredient" });
            }
        }

        #endregion
    }

    #region Data Transfer Objects
    
    public class IngredientCreateModel
    {
        public string Name { get; set; }
        public string Category { get; set; }
        public string Measurement { get; set; }
        public string Description { get; set; }
    }
    
    public class BulkActionOptions
    {
        public string CategoryName { get; set; }
        public string NewCategoryName { get; set; }
        public string MeasurementUnit { get; set; }
        public string ExportFormat { get; set; }
        public bool IncludeRecipeUsage { get; set; }
    }

    #endregion
}