using Microsoft.EntityFrameworkCore;
using MealStack.Infrastructure.Data;
using MealStack.Infrastructure.Data.Entities;
using MealStack.Web.Models;
using MealStack.Web.Services.Interface;

namespace MealStack.Web.Services
{
    public class MealPlanService : IMealPlanService
    {
        private readonly MealStackDbContext _context;

        public MealPlanService(MealStackDbContext context)
        {
            _context = context;
        }

        public async Task<List<MealPlanViewModel>> GetUserMealPlansAsync(string userId)
        {
            var entities = await _context.MealPlans
                .Where(mp => mp.UserId == userId)
                .OrderByDescending(mp => mp.CreatedDate)
                .ToListAsync();

            var result = new List<MealPlanViewModel>();
            foreach (var e in entities)
            {
                var mealCount = await _context.MealPlanItems.CountAsync(mi => mi.MealPlanId == e.Id);
                var shopCount = await _context.ShoppingListItems.CountAsync(si => si.MealPlanId == e.Id);
                result.Add(new MealPlanViewModel
                {
                    Id                 = e.Id,
                    Name               = e.Name,
                    Description        = e.Description,
                    StartDate          = e.StartDate,
                    EndDate            = e.EndDate,
                    UserId             = e.UserId,
                    CreatedDate        = e.CreatedDate,
                    MealItemsCount     = mealCount,
                    ShoppingItemsCount = shopCount
                });
            }
            return result;
        }

        public async Task<MealPlanViewModel> GetMealPlanByIdAsync(int id, string userId)
        {
            var e = await _context.MealPlans
                .Include(mp => mp.MealItems).ThenInclude(mi => mi.Recipe)
                .Include(mp => mp.ShoppingItems)
                .FirstOrDefaultAsync(mp => mp.Id == id && mp.UserId == userId);

            if (e == null) return null;

            return new MealPlanViewModel
            {
                Id          = e.Id,
                Name        = e.Name,
                Description = e.Description,
                StartDate   = e.StartDate,
                EndDate     = e.EndDate,
                UserId      = e.UserId,
                CreatedDate = e.CreatedDate,
                MealItems   = e.MealItems
                    .Select(mi => new MealPlanItemViewModel
                    {
                        Id              = mi.Id,
                        MealPlanId      = mi.MealPlanId,
                        RecipeId        = mi.RecipeId,
                        RecipeTitle     = mi.Recipe.Title,
                        RecipeImagePath = mi.Recipe.ImagePath,
                        PlannedDate     = mi.PlannedDate,
                        MealType        = mi.MealType,
                        Servings        = mi.Servings,
                        Notes           = mi.Notes
                    })
                    .OrderBy(mi => mi.PlannedDate)
                    .ThenBy(mi => mi.MealType)
                    .ToList(),
                ShoppingItems = e.ShoppingItems
                    .Select(si => new ShoppingListItemViewModel
                    {
                        Id             = si.Id,
                        MealPlanId     = si.MealPlanId,
                        IngredientName = si.IngredientName,
                        Quantity       = si.Quantity,
                        Unit           = si.Unit,
                        Category       = si.Category,
                        IsChecked      = si.IsChecked,
                        IngredientId   = si.IngredientId
                    })
                    .OrderBy(si => si.Category)
                    .ThenBy(si => si.IngredientName)
                    .ToList()
            };
        }

        public async Task<int> CreateMealPlanAsync(MealPlanViewModel model)
        {
            var entity = new MealPlanEntity
            {
                Name        = model.Name,
                Description = model.Description,

                // Force UTC kind on user‐entered dates:
                StartDate   = DateTime.SpecifyKind(model.StartDate, DateTimeKind.Utc),
                EndDate     = DateTime.SpecifyKind(model.EndDate,   DateTimeKind.Utc),

                UserId      = model.UserId,
                CreatedDate = DateTime.UtcNow
            };

            _context.MealPlans.Add(entity);
            await _context.SaveChangesAsync();

            model.Id = entity.Id;
            return entity.Id;
        }

        public async Task UpdateMealPlanAsync(MealPlanViewModel model)
        {
            var e = await _context.MealPlans
                .FirstOrDefaultAsync(mp => mp.Id == model.Id && mp.UserId == model.UserId);

            if (e == null)
                throw new InvalidOperationException("Meal plan not found or access denied.");

            e.Name        = model.Name;
            e.Description = model.Description;

            // Again, ensure UTC kind:
            e.StartDate   = DateTime.SpecifyKind(model.StartDate, DateTimeKind.Utc);
            e.EndDate     = DateTime.SpecifyKind(model.EndDate,   DateTimeKind.Utc);

            await _context.SaveChangesAsync();
        }

        public async Task<bool> DeleteMealPlanAsync(int id, string userId)
        {
            var e = await _context.MealPlans
                .FirstOrDefaultAsync(mp => mp.Id == id && mp.UserId == userId);
            if (e == null) return false;

            _context.MealPlans.Remove(e);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task GenerateShoppingListAsync(int mealPlanId, string userId)
        {
            var plan = await _context.MealPlans
                .Include(mp => mp.MealItems).ThenInclude(mi => mi.Recipe)
                .FirstOrDefaultAsync(mp => mp.Id == mealPlanId && mp.UserId == userId);
            if (plan == null) throw new InvalidOperationException("Meal plan not found or access denied.");

            // Remove existing
            var existing = await _context.ShoppingListItems
                .Where(si => si.MealPlanId == mealPlanId)
                .ToListAsync();
            _context.ShoppingListItems.RemoveRange(existing);

            var list = new List<ShoppingListItemEntity>();
            foreach (var mi in plan.MealItems)
            {
                if (string.IsNullOrWhiteSpace(mi.Recipe.Ingredients)) continue;
                var lines = mi.Recipe.Ingredients
                    .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                    .Select(l => l.Trim())
                    .Where(l => l != "");

                foreach (var line in lines)
                {
                    var parts = line.Split(' ', 3);
                    string qty = "", unit = "", name = line;
                    if (parts.Length >= 2 && float.TryParse(parts[0], out _))
                    {
                        qty = parts[0];
                        if (parts.Length == 3)
                        {
                            unit = parts[1];
                            name = parts[2];
                        }
                        else name = parts[1];
                    }

                    var dup = list.FirstOrDefault(ci =>
                        ci.IngredientName.Equals(name, StringComparison.OrdinalIgnoreCase));
                    if (dup != null)
                        dup.Quantity = "Multiple";
                    else
                        list.Add(new ShoppingListItemEntity
                        {
                            MealPlanId     = mealPlanId,
                            IngredientName = name,
                            Quantity       = qty,
                            Unit           = unit,
                            Category       = DetermineCategory(name),
                            IsChecked      = false
                        });
                }
            }

            _context.ShoppingListItems.AddRange(list);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateShoppingItemStatusAsync(int itemId, bool isChecked, string userId)
        {
            var si = await _context.ShoppingListItems
                .Include(x => x.MealPlan)
                .FirstOrDefaultAsync(x => x.Id == itemId && x.MealPlan.UserId == userId);
            if (si == null) throw new InvalidOperationException("Access denied.");

            si.IsChecked = isChecked;
            await _context.SaveChangesAsync();
        }

        private DateTime ParseAndNormalizeDate(string dateString)
        {
            if (DateTime.TryParse(dateString, out DateTime result))
            {
                // Ensure UTC kind for database storage
                return DateTime.SpecifyKind(result.Date, DateTimeKind.Utc);
            }
            
            // Try specific formats if standard parsing fails
            string[] formats = { "yyyy-MM-dd", "dd/MM/yyyy", "MM/dd/yyyy" };
            
            foreach (var format in formats)
            {
                if (DateTime.TryParseExact(dateString, format, 
                    System.Globalization.CultureInfo.InvariantCulture, 
                    System.Globalization.DateTimeStyles.None, out result))
                {
                    return DateTime.SpecifyKind(result.Date, DateTimeKind.Utc);
                }
            }
            
            // If all parsing attempts fail, use current date
            return DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);
        }

        // Then in AddMealItemAsync method:
        public async Task<MealPlanItemViewModel> AddMealItemAsync(MealPlanItemViewModel model)
        {
            var plan = await _context.MealPlans
                .FirstOrDefaultAsync(mp => mp.Id == model.MealPlanId && mp.UserId == model.UserId);
            if (plan == null) throw new InvalidOperationException("Access denied.");

            var recipe = await _context.Recipes.FindAsync(model.RecipeId)
                         ?? throw new InvalidOperationException("Recipe not found.");

            // Ensure the date is within the meal plan date range
            // Force UTC kind on user‐entered dates:
            var plannedDate = model.PlannedDate;
            
            // Check if the date is within plan range
            if (plannedDate.Date < plan.StartDate.Date || plannedDate.Date > plan.EndDate.Date)
            {
                throw new InvalidOperationException($"The selected date must be between {plan.StartDate:d} and {plan.EndDate:d}");
            }

            var entity = new MealPlanItemEntity
            {
                MealPlanId  = model.MealPlanId,
                RecipeId    = model.RecipeId,
                PlannedDate = DateTime.SpecifyKind(plannedDate.Date, DateTimeKind.Utc),
                MealType    = model.MealType,
                Servings    = model.Servings,
                Notes       = model.Notes
            };
            
            _context.MealPlanItems.Add(entity);
            await _context.SaveChangesAsync();

            model.Id               = entity.Id;
            model.RecipeTitle      = recipe.Title;
            model.RecipeImagePath  = recipe.ImagePath;
            return model;
        }

        public async Task<MealPlanItemViewModel> UpdateMealItemAsync(MealPlanItemViewModel model)
        {
            var mi = await _context.MealPlanItems
                .Include(x => x.MealPlan).Include(x => x.Recipe)
                .FirstOrDefaultAsync(x => x.Id == model.Id && x.MealPlan.UserId == model.UserId);
            if (mi == null) throw new InvalidOperationException("Access denied.");

            mi.PlannedDate = model.PlannedDate;
            mi.MealType    = model.MealType;
            mi.Servings    = model.Servings;
            mi.Notes       = model.Notes;
            await _context.SaveChangesAsync();

            model.RecipeTitle     = mi.Recipe.Title;
            model.RecipeImagePath = mi.Recipe.ImagePath;
            return model;
        }

        public async Task RemoveMealItemAsync(int itemId, string userId)
        {
            var mi = await _context.MealPlanItems
                .Include(x => x.MealPlan)
                .FirstOrDefaultAsync(x => x.Id == itemId && x.MealPlan.UserId == userId);
            if (mi == null) throw new InvalidOperationException("Access denied.");

            _context.MealPlanItems.Remove(mi);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> UserHasAccessToMealPlanAsync(int mealPlanId, string userId)
        {
            return await _context.MealPlans
                .AnyAsync(mp => mp.Id == mealPlanId && mp.UserId == userId);
        }

        public async Task<ShoppingListItemViewModel> AddShoppingItemAsync(ShoppingListItemViewModel model, string userId)
        {
            var plan = await _context.MealPlans
                .FirstOrDefaultAsync(mp => mp.Id == model.MealPlanId && mp.UserId == userId);
            if (plan == null) throw new InvalidOperationException("Access denied.");

            var entity = new ShoppingListItemEntity
            {
                MealPlanId     = model.MealPlanId,
                IngredientName = model.IngredientName,
                Quantity       = model.Quantity,
                Unit           = model.Unit,
                Category       = model.Category,
                IngredientId   = model.IngredientId,
                IsChecked      = model.IsChecked
            };
            _context.ShoppingListItems.Add(entity);
            await _context.SaveChangesAsync();

            model.Id = entity.Id;
            return model;
        }

        public async Task RemoveShoppingItemAsync(int itemId, string userId)
        {
            var si = await _context.ShoppingListItems
                .Include(x => x.MealPlan)
                .FirstOrDefaultAsync(x => x.Id == itemId && x.MealPlan.UserId == userId);
            if (si == null) throw new InvalidOperationException("Access denied.");

            _context.ShoppingListItems.Remove(si);
            await _context.SaveChangesAsync();
        }
        
        private string DetermineCategory(string name)
        {
            var n = name.ToLowerInvariant();
            if (new[] { "flour","sugar","rice","pasta","oats","cereal" }.Any(x => n.Contains(x))) 
                return "Pantry";
            if (new[] { "chicken","beef","pork","fish","meat","seafood" }.Any(x => n.Contains(x))) 
                return "Meat & Seafood";
            if (new[] { "milk","cheese","yogurt","cream","butter","egg" }.Any(x => n.Contains(x))) 
                return "Dairy & Eggs";
            if (new[] { "apple","banana","orange","berry","fruit" }.Any(x => n.Contains(x))) 
                return "Fruits";
            if (new[] { "carrot","potato","onion","garlic","vegetable","tomato","lettuce","pepper" }.Any(x => n.Contains(x))) 
                return "Vegetables";
            if (new[] { "salt","pepper","spice","herb","basil","oregano","thyme","cilantro" }.Any(x => n.Contains(x))) 
                return "Spices & Herbs";
            if (new[] { "oil","vinegar","sauce","ketchup","mustard","mayonnaise" }.Any(x => n.Contains(x))) 
                return "Condiments & Oils";
            return "Other";
        }
        
        public async Task BulkUpdateShoppingItemsAsync(int mealPlanId, string action, string userId, int[] itemIds = null)
        {
            var plan = await _context.MealPlans
                .FirstOrDefaultAsync(mp => mp.Id == mealPlanId && mp.UserId == userId);
            
            if (plan == null)
                throw new InvalidOperationException("Meal plan not found or access denied.");

            switch (action.ToLower())
            {
                case "clearall":
                    await ClearAllCheckedItemsAsync(mealPlanId, userId);
                    break;
                case "checkall":
                    await CheckAllItemsAsync(mealPlanId, userId);
                    break;
                case "uncheckall":
                    await UncheckAllItemsAsync(mealPlanId, userId);
                    break;
                case "checkselected":
                    await CheckSelectedItemsAsync(itemIds, userId);
                    break;
                case "uncheckselected":
                    await UncheckSelectedItemsAsync(itemIds, userId);
                    break;
                case "deleteselected":
                    await DeleteSelectedItemsAsync(itemIds, userId);
                    break;
                default:
                    throw new ArgumentException($"Unknown action: {action}");
            }
        }

        public async Task<List<ShoppingListItemViewModel>> AddMultipleShoppingItemsAsync(
            int mealPlanId, string itemsText, string userId)
        {
            var plan = await _context.MealPlans
                .FirstOrDefaultAsync(mp => mp.Id == mealPlanId && mp.UserId == userId);
            
            if (plan == null)
                throw new InvalidOperationException("Meal plan not found or access denied.");

            var items = ParseMultipleItemsText(itemsText);
            var addedItems = new List<ShoppingListItemViewModel>();

            foreach (var item in items)
            {
                item.MealPlanId = mealPlanId;
                
                var entity = new ShoppingListItemEntity
                {
                    MealPlanId = mealPlanId,
                    IngredientName = item.IngredientName,
                    Quantity = item.Quantity,
                    Unit = item.Unit,
                    Category = item.Category,
                    IsChecked = false
                };

                _context.ShoppingListItems.Add(entity);
                await _context.SaveChangesAsync();

                item.Id = entity.Id;
                addedItems.Add(item);
            }

            return addedItems;
        }

        private async Task ClearAllCheckedItemsAsync(int mealPlanId, string userId)
        {
            var checkedItems = await _context.ShoppingListItems
                .Include(s => s.MealPlan)
                .Where(s => s.MealPlanId == mealPlanId && 
                           s.MealPlan.UserId == userId && 
                           s.IsChecked)
                .ToListAsync();

            _context.ShoppingListItems.RemoveRange(checkedItems);
            await _context.SaveChangesAsync();
        }

        private async Task CheckAllItemsAsync(int mealPlanId, string userId)
        {
            var items = await _context.ShoppingListItems
                .Include(s => s.MealPlan)
                .Where(s => s.MealPlanId == mealPlanId && 
                           s.MealPlan.UserId == userId && 
                           !s.IsChecked)
                .ToListAsync();

            foreach (var item in items)
            {
                item.IsChecked = true;
            }

            await _context.SaveChangesAsync();
        }

        private async Task UncheckAllItemsAsync(int mealPlanId, string userId)
        {
            var items = await _context.ShoppingListItems
                .Include(s => s.MealPlan)
                .Where(s => s.MealPlanId == mealPlanId && 
                           s.MealPlan.UserId == userId && 
                           s.IsChecked)
                .ToListAsync();

            foreach (var item in items)
            {
                item.IsChecked = false;
            }

            await _context.SaveChangesAsync();
        }

        private async Task CheckSelectedItemsAsync(int[] itemIds, string userId)
        {
            if (itemIds == null || itemIds.Length == 0) return;

            var items = await _context.ShoppingListItems
                .Include(s => s.MealPlan)
                .Where(s => itemIds.Contains(s.Id) && 
                           s.MealPlan.UserId == userId)
                .ToListAsync();

            foreach (var item in items)
            {
                item.IsChecked = true;
            }

            await _context.SaveChangesAsync();
        }

        private async Task UncheckSelectedItemsAsync(int[] itemIds, string userId)
        {
            if (itemIds == null || itemIds.Length == 0) return;

            var items = await _context.ShoppingListItems
                .Include(s => s.MealPlan)
                .Where(s => itemIds.Contains(s.Id) && 
                           s.MealPlan.UserId == userId)
                .ToListAsync();

            foreach (var item in items)
            {
                item.IsChecked = false;
            }

            await _context.SaveChangesAsync();
        }

        private async Task DeleteSelectedItemsAsync(int[] itemIds, string userId)
        {
            if (itemIds == null || itemIds.Length == 0) return;

            var items = await _context.ShoppingListItems
                .Include(s => s.MealPlan)
                .Where(s => itemIds.Contains(s.Id) && 
                           s.MealPlan.UserId == userId)
                .ToListAsync();

            _context.ShoppingListItems.RemoveRange(items);
            await _context.SaveChangesAsync();
        }

        private List<ShoppingListItemViewModel> ParseMultipleItemsText(string itemsText)
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

                var item = ParseSingleItemLine(trimmedLine);
                if (item != null)
                {
                    items.Add(item);
                }
            }

            return items;
        }

        private ShoppingListItemViewModel ParseSingleItemLine(string line)
        {
            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return null;

            var item = new ShoppingListItemViewModel();

            // Try to parse quantity and unit
            if (parts.Length >= 2 && IsNumeric(parts[0]))
            {
                item.Quantity = parts[0];
                
                // Check if second part is a unit
                if (parts.Length >= 3 && IsUnit(parts[1]))
                {
                    item.Unit = parts[1];
                    item.IngredientName = string.Join(" ", parts.Skip(2));
                }
                else
                {
                    item.IngredientName = string.Join(" ", parts.Skip(1));
                }
            }
            else
            {
                // No quantity, just ingredient name
                item.IngredientName = line;
            }

            item.Category = DetermineItemCategory(item.IngredientName);
            return item;
        }

        private string DetermineItemCategory(string ingredientName)
        {
            if (string.IsNullOrEmpty(ingredientName))
                return "Other";

            ingredientName = ingredientName.ToLower();

            if (ingredientName.Contains("milk") || 
                ingredientName.Contains("cheese") || 
                ingredientName.Contains("yogurt") ||
                ingredientName.Contains("butter") ||
                ingredientName.Contains("cream"))
                return "Dairy";

            if (ingredientName.Contains("beef") || 
                ingredientName.Contains("chicken") || 
                ingredientName.Contains("pork") ||
                ingredientName.Contains("fish") ||
                ingredientName.Contains("meat") ||
                ingredientName.Contains("turkey"))
                return "Meat";

            if (ingredientName.Contains("apple") || 
                ingredientName.Contains("banana") || 
                ingredientName.Contains("orange") ||
                ingredientName.Contains("berry") ||
                ingredientName.Contains("fruit"))
                return "Fruits";

            if (ingredientName.Contains("carrot") || 
                ingredientName.Contains("onion") || 
                ingredientName.Contains("potato") ||
                ingredientName.Contains("tomato") ||
                ingredientName.Contains("vegetable"))
                return "Vegetables";

            if (ingredientName.Contains("bread") || 
                ingredientName.Contains("pasta") || 
                ingredientName.Contains("flour") ||
                ingredientName.Contains("rice") ||
                ingredientName.Contains("cereal"))
                return "Grains";

            if (ingredientName.Contains("sugar") || 
                ingredientName.Contains("salt") || 
                ingredientName.Contains("pepper") ||
                ingredientName.Contains("spice") ||
                ingredientName.Contains("herb"))
                return "Spices";

            return "Other";
        }

        private bool IsNumeric(string value)
        {
            return decimal.TryParse(value, out _) || 
                   value.Contains('/') || // fractions like "1/2"
                   value.Contains('.'); // decimals
        }

        private bool IsUnit(string value)
        {
            var commonUnits = new[]
            {
                "cup", "cups", "tsp", "tbsp", "tablespoon", "tablespoons", "teaspoon", "teaspoons",
                "oz", "ounce", "ounces", "lb", "lbs", "pound", "pounds", "kg", "kilogram", "kilograms",
                "g", "gram", "grams", "ml", "milliliter", "milliliters", "l", "liter", "liters",
                "piece", "pieces", "slice", "slices", "can", "cans", "bottle", "bottles",
                "package", "packages", "box", "boxes", "bag", "bags", "large", "medium", "small"
            };

            return commonUnits.Contains(value.ToLowerInvariant());
        }

        public async Task<ShoppingListStatistics> GetShoppingListStatisticsAsync(int mealPlanId, string userId)
        {
            var items = await _context.ShoppingListItems
                .Include(s => s.MealPlan)
                .Where(s => s.MealPlanId == mealPlanId && s.MealPlan.UserId == userId)
                .ToListAsync();

            return new ShoppingListStatistics
            {
                TotalItems = items.Count,
                CheckedItems = items.Count(i => i.IsChecked),
                UncheckedItems = items.Count(i => !i.IsChecked),
                CategoriesCount = items.GroupBy(i => i.Category ?? "Other").Count(),
                CompletionPercentage = items.Count > 0 ? 
                    Math.Round((double)items.Count(i => i.IsChecked) / items.Count * 100, 1) : 0
            };
        }

        // Helper class for statistics
        public class ShoppingListStatistics
        {
            public int TotalItems { get; set; }
            public int CheckedItems { get; set; }
            public int UncheckedItems { get; set; }
            public int CategoriesCount { get; set; }
            public double CompletionPercentage { get; set; }
        }
    }
}
