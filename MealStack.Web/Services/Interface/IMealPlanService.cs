using MealStack.Web.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MealStack.Web.Services.Interfaces
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
        Task RemoveMealItemAsync(int itemId, string userId);
    }
}