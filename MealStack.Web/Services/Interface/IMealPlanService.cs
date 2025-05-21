using MealStack.Web.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MealStack.Web.Services.Interface
{
    public interface IMealPlanService
    {
        Task<List<MealPlanViewModel>> GetUserMealPlansAsync(string userId);
        Task<MealPlanViewModel> GetMealPlanByIdAsync(int id, string userId);
        Task<int> CreateMealPlanAsync(MealPlanViewModel model);
        Task UpdateMealPlanAsync(MealPlanViewModel model);
        Task<bool> DeleteMealPlanAsync(int id, string userId);
        Task GenerateShoppingListAsync(int mealPlanId, string userId);
        Task UpdateShoppingItemStatusAsync(int itemId, bool isChecked, string userId);
        Task<MealPlanItemViewModel> AddMealItemAsync(MealPlanItemViewModel model);
        Task<MealPlanItemViewModel> UpdateMealItemAsync(MealPlanItemViewModel model);
        Task<bool> UserHasAccessToMealPlanAsync(int mealPlanId, string userId);
        Task<ShoppingListItemViewModel> AddShoppingItemAsync(ShoppingListItemViewModel model, string userId);
        Task RemoveMealItemAsync(int itemId, string userId);
    }
}