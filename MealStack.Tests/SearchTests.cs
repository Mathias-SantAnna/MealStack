using FluentAssertions;
using MealStack.Infrastructure.Data;

namespace MealStack.Tests
{
    public class SearchTests : IDisposable
    {
        private readonly TestHelper _helper = new();

        [Fact]
        public void Can_Search_By_Ingredients()
        {
            // Given recipes with different ingredients
            var user = _helper.CreateUser("SearchChef");
            var chickenRecipe = _helper.CreateRecipe(user, "Chicken Curry");
            chickenRecipe.Ingredients = "2 lbs chicken\n1 cup rice\n2 tbsp curry powder";
            
            var veggieRecipe = _helper.CreateRecipe(user, "Veggie Stir Fry");
            veggieRecipe.Ingredients = "1 cup broccoli\n1 cup carrots\n2 tbsp soy sauce";

            _helper.DbContext.Users.Add(user);
            _helper.DbContext.Recipes.AddRange(chickenRecipe, veggieRecipe);
            _helper.DbContext.SaveChanges();

            // When searching for "chicken"
            var chickenRecipes = _helper.DbContext.Recipes
                .Where(r => r.Ingredients.Contains("chicken"))
                .ToList();

            // Then should find only chicken recipe
            chickenRecipes.Should().HaveCount(1);
            chickenRecipes.First().Title.Should().Be("Chicken Curry");
        }

        [Fact]
        public void Can_Search_By_Difficulty()
        {
            // Given recipes with different difficulties
            var user = _helper.CreateUser("DifficultyTester");
            var easyRecipe = _helper.CreateRecipe(user, "Toast");
            easyRecipe.Difficulty = DifficultyLevel.Easy;
            
            var hardRecipe = _helper.CreateRecipe(user, "Beef Wellington");
            hardRecipe.Difficulty = DifficultyLevel.Masterchef;

            _helper.DbContext.Users.Add(user);
            _helper.DbContext.Recipes.AddRange(easyRecipe, hardRecipe);
            _helper.DbContext.SaveChanges();

            // When searching for easy recipes
            var easyRecipes = _helper.DbContext.Recipes
                .Where(r => r.Difficulty == DifficultyLevel.Easy)
                .ToList();

            // Then should find only easy recipe
            easyRecipes.Should().HaveCount(1);
            easyRecipes.First().Title.Should().Be("Toast");
        }

        [Fact]
        public void Can_Search_By_Cook_Time()
        {
            // Given recipes with different cook times
            var user = _helper.CreateUser("TimeWatcher");
            var quickRecipe = _helper.CreateRecipe(user, "5 Minute Salad");
            quickRecipe.CookTimeMinutes = 5;
            
            var slowRecipe = _helper.CreateRecipe(user, "Slow Roast");
            slowRecipe.CookTimeMinutes = 180; // 3 hours

            _helper.DbContext.Users.Add(user);
            _helper.DbContext.Recipes.AddRange(quickRecipe, slowRecipe);
            _helper.DbContext.SaveChanges();

            // When searching for quick recipes (under 30 minutes)
            var quickRecipes = _helper.DbContext.Recipes
                .Where(r => r.CookTimeMinutes <= 30)
                .ToList();

            // Then should find only quick recipe
            quickRecipes.Should().HaveCount(1);
            quickRecipes.First().Title.Should().Be("5 Minute Salad");
        }

        public void Dispose() => _helper.Dispose();
    }
}