using FluentAssertions;
using MealStack.Infrastructure.Data;
using MealStack.Infrastructure.Data.Entities;

namespace MealStack.Tests
{
    public class MealPlanTests : IDisposable
    {
        private readonly TestHelper _helper = new();

        [Fact]
        public void User_Can_Create_Meal_Plan()
        {
            // Given a user who wants to plan meals
            var user = _helper.CreateUser("Planner");
            _helper.DbContext.Users.Add(user);
            _helper.DbContext.SaveChanges();

            // When they create a meal plan
            var mealPlan = new MealPlanEntity
            {
                Name = "This Week's Meals",
                Description = "Healthy meals for the week",
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddDays(7),
                UserId = user.Id,
                CreatedDate = DateTime.UtcNow
            };
            _helper.DbContext.MealPlans.Add(mealPlan);
            _helper.DbContext.SaveChanges();

            // Then the meal plan should exist
            var savedPlan = _helper.DbContext.MealPlans.First();
            savedPlan.Name.Should().Be("This Week's Meals");
            savedPlan.UserId.Should().Be(user.Id);
        }

        [Fact]
        public void Meal_Plan_Can_Have_Recipes()
        {
            // Given a meal plan and some recipes
            var user = _helper.CreateUser("MealPrepper");
            var recipe1 = _helper.CreateRecipe(user, "Breakfast Oats");
            var recipe2 = _helper.CreateRecipe(user, "Lunch Salad");
            
            var mealPlan = new MealPlanEntity
            {
                Name = "Healthy Week",
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddDays(7),
                UserId = user.Id,
                CreatedDate = DateTime.UtcNow
            };

            _helper.DbContext.Users.Add(user);
            _helper.DbContext.Recipes.AddRange(recipe1, recipe2);
            _helper.DbContext.MealPlans.Add(mealPlan);
            _helper.DbContext.SaveChanges();

            // When adding recipes to the meal plan
            var mealItems = new[]
            {
                new MealPlanItemEntity
                {
                    MealPlanId = mealPlan.Id,
                    RecipeId = recipe1.Id,
                    PlannedDate = DateTime.Today.AddDays(1),
                    MealType = MealType.Breakfast,
                    Servings = 2
                },
                new MealPlanItemEntity
                {
                    MealPlanId = mealPlan.Id,
                    RecipeId = recipe2.Id,
                    PlannedDate = DateTime.Today.AddDays(1),
                    MealType = MealType.Lunch,
                    Servings = 1
                }
            };
            _helper.DbContext.MealPlanItems.AddRange(mealItems);
            _helper.DbContext.SaveChanges();

            // Then the meal plan should have 2 meals
            var planWithMeals = _helper.DbContext.MealPlanItems
                .Where(item => item.MealPlanId == mealPlan.Id)
                .ToList();
            
            planWithMeals.Should().HaveCount(2);
        }

        public void Dispose() => _helper.Dispose();
    }
}