using Microsoft.EntityFrameworkCore;
using MealStack.Infrastructure.Data;
using MealStack.Infrastructure.Data.Entities;
using MealStack.Web.Models;
using MealStack.Web.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

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
            return await _context.MealPlans
                .Where(mp => mp.UserId == userId)
                .OrderByDescending(mp => mp.StartDate)
                .Select(mp => new MealPlanViewModel
                {
                    Id = mp.Id,
                    Name = mp.Name,
                    Description = mp.Description,
                    StartDate = mp.StartDate,
                    EndDate = mp.EndDate,
                    UserId = mp.UserId,
                    CreatedDate = mp.CreatedDate,
                    MealItemsCount = mp.MealItems.Count,
                    ShoppingItemsCount = mp.ShoppingItems.Count(si => !si.IsChecked)
                })
                .ToListAsync();
        }

        public async Task<MealPlanViewModel> GetMealPlanByIdAsync(int id, string userId)
        {
            var mealPlanEntity = await _context.MealPlans
                .Include(mp => mp.MealItems)
                    .ThenInclude(mi => mi.Recipe)
                .Include(mp => mp.ShoppingItems)
                .FirstOrDefaultAsync(mp => mp.Id == id && mp.UserId == userId);

            if (mealPlanEntity == null)
                return null;

            return new MealPlanViewModel
            {
                Id = mealPlanEntity.Id,
                Name = mealPlanEntity.Name,
                Description = mealPlanEntity.Description,
                StartDate = mealPlanEntity.StartDate,
                EndDate = mealPlanEntity.EndDate,
                UserId = mealPlanEntity.UserId,
                CreatedDate = mealPlanEntity.CreatedDate,
                MealItems = mealPlanEntity.MealItems
                    .OrderBy(mi => mi.PlannedDate)
                    .ThenBy(mi => mi.MealType)
                    .Select(mi => new MealPlanItemViewModel
                    {
                        Id = mi.Id,
                        MealPlanId = mi.MealPlanId,
                        RecipeId = mi.RecipeId,
                        RecipeTitle = mi.Recipe?.Title,
                        RecipeImagePath = mi.Recipe?.ImagePath,
                        PlannedDate = mi.PlannedDate,
                        MealType = mi.MealType,
                        Servings = mi.Servings,
                        Notes = mi.Notes,
                        UserId = mealPlanEntity.UserId 
                    }).ToList(),
                ShoppingItems = mealPlanEntity.ShoppingItems
                    .OrderBy(si => si.Category)
                    .ThenBy(si => si.IngredientName)
                    .Select(si => new ShoppingListItemViewModel
                    {
                        Id = si.Id,
                        MealPlanId = si.MealPlanId,
                        IngredientName = si.IngredientName,
                        Quantity = si.Quantity, 
                        Unit = si.Unit,
                        Category = si.Category,
                        IsChecked = si.IsChecked,
                        IngredientId = si.IngredientId
                    }).ToList()
            };
        }

        public async Task<int> CreateMealPlanAsync(MealPlanViewModel model)
        {
            var entity = new MealPlanEntity 
            {
                Name = model.Name,
                Description = model.Description,
                StartDate = model.StartDate.Date,
                EndDate = model.EndDate.Date,
                UserId = model.UserId,
                CreatedDate = DateTime.UtcNow
            };

            _context.MealPlans.Add(entity);
            await _context.SaveChangesAsync();

            model.Id = entity.Id; 
            return entity.Id;
        }

        public async Task UpdateMealPlanAsync(MealPlanViewModel model)
        {
            var entity = await _context.MealPlans
                .FirstOrDefaultAsync(mp => mp.Id == model.Id && mp.UserId == model.UserId);

            if (entity == null)
                throw new Exception("Meal plan not found or you don't have permission to edit it.");

            entity.Name = model.Name;
            entity.Description = model.Description;
            entity.StartDate = model.StartDate.Date;
            entity.EndDate = model.EndDate.Date;

            await _context.SaveChangesAsync();
        }

        public async Task<bool> DeleteMealPlanAsync(int id, string userId)
        {
            var entity = await _context.MealPlans
                .FirstOrDefaultAsync(mp => mp.Id == id && mp.UserId == userId);

            if (entity == null)
                return false;

            _context.MealPlans.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task GenerateShoppingListAsync(int mealPlanId, string userId)
        {
            var mealPlan = await _context.MealPlans
                .Include(mp => mp.MealItems)
                    .ThenInclude(mi => mi.Recipe) 
                .FirstOrDefaultAsync(mp => mp.Id == mealPlanId && mp.UserId == userId);

            if (mealPlan == null)
                throw new Exception("Meal plan not found or you don't have permission to access it.");

            // Clear existing shopping items for this meal plan
            var existingItems = await _context.ShoppingListItems 
                .Where(si => si.MealPlanId == mealPlanId)
                .ToListAsync();
            _context.ShoppingListItems.RemoveRange(existingItems); 

            var aggregatedItems = new Dictionary<string, ShoppingListItemEntity>();

            foreach (var mealItem in mealPlan.MealItems)
            {
                if (mealItem.Recipe == null || string.IsNullOrEmpty(mealItem.Recipe.Ingredients))
                    continue;

                var recipeServings = mealItem.Recipe.Servings > 0 ? mealItem.Recipe.Servings : 1; 
                var plannedServings = mealItem.Servings;
                var servingRatio = (double)plannedServings / recipeServings;

                var ingredientLines = mealItem.Recipe.Ingredients.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in ingredientLines)
                {
                    ParseIngredientLine(line, servingRatio, aggregatedItems, mealPlanId);
                }
            }

            if (aggregatedItems.Any())
            {
                await _context.ShoppingListItems.AddRangeAsync(aggregatedItems.Values);
            }
            await _context.SaveChangesAsync();
        }

        private void ParseIngredientLine(string line, double servingRatio, Dictionary<string, ShoppingListItemEntity> aggregatedItems, int mealPlanId)
        {
            line = line.Trim();
            if (string.IsNullOrEmpty(line)) return;

            // Regex to capture: (1) quantity (digits, dots, slashes), (2) unit (optional, letters), (3) ingredient name
            var match = Regex.Match(line, @"^([\d./\s]+(?:[\d./]+)?)\s*([a-zA-Z]+(?:\s+[a-zA-Z]+)*)?\s+(.+)$");

            string quantityStrInput;
            string unitStr;
            string ingredientName;
            double quantityValue = 0;

            if (match.Success)
            {
                quantityStrInput = match.Groups[1].Value.Trim();
                unitStr = match.Groups[2].Value.Trim();
                ingredientName = match.Groups[3].Value.Trim();

                // Try to parse quantity (eg "1", "1.5", "1/2", "1 1/2")
                if (TryParseQuantity(quantityStrInput, out quantityValue))
                {
                    quantityValue *= servingRatio;
                }
                else 
                {
                     ingredientName = line;
                     quantityStrInput = ""; 
                     unitStr = "";
                     quantityValue = 0;
                }
            }
            else
            {
                // No numeric quantity detected at the start, treat whole line as ingredient name
                ingredientName = line;
                quantityStrInput = "";
                unitStr = "";
                quantityValue = 0; 
            }
            
            // Format quantity for storage (if it was numeric and scaled)
            string finalQuantityString;
            if (quantityValue > 0)
            {
                 finalQuantityString = quantityValue % 1 == 0 ? ((int)quantityValue).ToString() : quantityValue.ToString("0.##");
            }
            else
            {
                finalQuantityString = quantityStrInput;
            }


            string category = DetermineCategory(ingredientName);
            string key = $"{ingredientName.ToLowerInvariant()}_{unitStr?.ToLowerInvariant() ?? ""}";

            if (aggregatedItems.TryGetValue(key, out var existingItem))
            {
                if (double.TryParse(existingItem.Quantity, out double existingQtyValue) && quantityValue > 0)
                {
                    existingQtyValue += quantityValue;
                    existingItem.Quantity = existingQtyValue % 1 == 0 ? ((int)existingQtyValue).ToString() : existingQtyValue.ToString("0.##");
                }
                else if (!string.IsNullOrEmpty(finalQuantityString))
                {
                     // This case is tricky: "1 apple" + "a pinch of salt".
                     // If we already have a numeric amount and the new item is also numeric, we just add them together.  
                     // If one is numeric and the other isn’t (or neither is), leave the original alone and don’t update the amount.
                     // But for now this keeps things simple.
                }
            }
            else
            {
                aggregatedItems[key] = new ShoppingListItemEntity
                {
                    MealPlanId = mealPlanId,
                    IngredientName = ingredientName,
                    Quantity = finalQuantityString,
                    Unit = unitStr,
                    Category = category,
                    IsChecked = false
                };
            }
        }
        
        // Helper to parse complex quantity strings like "1 1/2"
        private bool TryParseQuantity(string quantityStr, out double result)
        {
            result = 0;
            quantityStr = quantityStr.Trim();
            double totalQuantity = 0;

            // Split by space to handle whole numbers and fractions separately 
            var parts = quantityStr.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            bool success = false;

            foreach (var part in parts)
            {
                if (part.Contains("/"))
                {
                    var fractionParts = part.Split('/');
                    if (fractionParts.Length == 2 && double.TryParse(fractionParts[0], out double num) && double.TryParse(fractionParts[1], out double den) && den != 0)
                    {
                        totalQuantity += num / den;
                        success = true;
                    }
                    else return false; 
                }
                else if (double.TryParse(part, out double decimalOrWhole)) 
                {
                    totalQuantity += decimalOrWhole;
                    success = true;
                }
                else return false; 
            }
            
            if(success) result = totalQuantity;
            return success;
        }


        private string DetermineCategory(string ingredientName)
        {
            string lowerName = ingredientName.ToLowerInvariant();
            
            if (new[] { "flour", "sugar", "rice", "pasta", "cereal", "baking soda", "yeast" }.Any(s => lowerName.Contains(s))) return "Pantry";
            if (new[] { "chicken", "beef", "pork", "fish", "turkey", "lamb", "salmon", "shrimp" }.Any(s => lowerName.Contains(s))) return "Meat & Seafood";
            if (new[] { "milk", "cheese", "yogurt", "cream", "butter", "egg" }.Any(s => lowerName.Contains(s))) return "Dairy & Eggs"; 
            if (new[] { "apple", "banana", "orange", "berry", "berries", "grape", "mango", "avocado" }.Any(s => lowerName.Contains(s))) return "Fruits";
            if (new[] { "lettuce", "spinach", "carrot", "onion", "potato", "tomato", "broccoli", "garlic", "pepper" }.Any(s => lowerName.Contains(s))) return "Vegetables";
            if (new[] { "salt", "pepper", "spice", "herb", "seasoning", "cinnamon", "oregano", "basil" }.Any(s => lowerName.Contains(s))) return "Spices & Herbs";
            if (new[] { "oil", "vinegar", "sauce", "dressing", "ketchup", "mustard", "soy sauce" }.Any(s => lowerName.Contains(s))) return "Condiments & Oils";
            return "Other";
        }

        public async Task UpdateShoppingItemStatusAsync(int itemId, bool isChecked, string userId)
        {
            var item = await _context.ShoppingListItems 
                .Include(si => si.MealPlan)
                .FirstOrDefaultAsync(si => si.Id == itemId && si.MealPlan.UserId == userId);

            if (item == null)
                throw new Exception("Shopping item not found or you don't have permission to update it.");

            item.IsChecked = isChecked;
            await _context.SaveChangesAsync();
        }

        public async Task<MealPlanItemViewModel> AddMealItemAsync(MealPlanItemViewModel model)
        {
            var mealPlan = await _context.MealPlans
                .FirstOrDefaultAsync(mp => mp.Id == model.MealPlanId && mp.UserId == model.UserId);

            if (mealPlan == null)
                throw new Exception("Meal plan not found or you don't have permission to add meals.");

            var recipe = await _context.Recipes.FindAsync(model.RecipeId);
            if (recipe == null)
                throw new Exception("Recipe not found.");

            var entity = new MealPlanItemEntity 
            {
                MealPlanId = model.MealPlanId,
                RecipeId = model.RecipeId,
                PlannedDate = model.PlannedDate.Date,
                MealType = model.MealType,
                Servings = model.Servings,
                Notes = model.Notes
            };

            _context.MealPlanItems.Add(entity);
            await _context.SaveChangesAsync();
            
            model.Id = entity.Id;
            model.RecipeTitle = recipe.Title;
            model.RecipeImagePath = recipe.ImagePath;
            return model;
        }

        public async Task RemoveMealItemAsync(int itemId, string userId)
        {
            var item = await _context.MealPlanItems
                .Include(mi => mi.MealPlan) 
                .FirstOrDefaultAsync(mi => mi.Id == itemId && mi.MealPlan.UserId == userId);

            if (item == null)
                throw new Exception("Meal item not found or you don't have permission to remove it.");

            _context.MealPlanItems.Remove(item);
            await _context.SaveChangesAsync();
        }
    }
}