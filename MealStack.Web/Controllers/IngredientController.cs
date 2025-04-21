using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MealStack.Infrastructure.Data;
using MealStack.Infrastructure.Data.Entities;

namespace MealStack.Web.Controllers
{
    public class IngredientController : Controller
    {
        private readonly MealStackDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public IngredientController(MealStackDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Ingredient
        public async Task<IActionResult> Index()
        {
            var ingredients = await _context.Ingredients.ToListAsync();
            return View(ingredients);
        }

        // API endpoint to get all ingredients for autocomplete
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

        // API endpoint to search ingredients
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

        // API endpoint to add a new ingredient via AJAX
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> AddIngredientAjax([FromBody] IngredientEntity ingredient)
        {
            if (string.IsNullOrEmpty(ingredient.Name))
            {
                return BadRequest(new { success = false, message = "Ingredient name is required" });
            }
            
            // Set required fields
            ingredient.CreatedById = _userManager.GetUserId(User);
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
                    category = ingredient.Category,
                    measurement = ingredient.Measurement,
                    description = ingredient.Description
                }
            });
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
            return View();
        }

        // POST: Ingredient/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Create([Bind("Name,Description,Category,Measurement")] IngredientEntity ingredient)
        {
            if (ModelState.IsValid)
            {
                if (User.Identity.IsAuthenticated)
                {
                    ingredient.CreatedById = _userManager.GetUserId(User);
                }
                
                ingredient.CreatedDate = DateTime.UtcNow;
                _context.Add(ingredient);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(ingredient);
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

            // Only allow edit if user created the ingredient or is admin
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

            // Only allow edit if user created the ingredient or is admin
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

            // Only allow delete if user created the ingredient or is admin
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

            // Only allow delete if user created the ingredient or is admin
            if (User.Identity.IsAuthenticated && 
               (ingredient.CreatedById == _userManager.GetUserId(User) || User.IsInRole("Admin")))
            {
                _context.Ingredients.Remove(ingredient);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            
            return Forbid();
        }
    }
}