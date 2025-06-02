using FluentAssertions;
using MealStack.Infrastructure.Data.Entities;

namespace MealStack.Tests
{
    public class RatingTests : IDisposable
    {
        private readonly TestHelper _helper = new();

        [Fact]
        public void Recipe_With_No_Ratings_Has_Zero_Average()
        {
            // Given a new recipe with no ratings
            var user = _helper.CreateUser("NewChef");
            var recipe = _helper.CreateRecipe(user, "Untested Recipe");
            
            _helper.DbContext.Users.Add(user);
            _helper.DbContext.Recipes.Add(recipe);
            _helper.DbContext.SaveChanges();

            // When checking the rating
            var recipeWithRatings = _helper.DbContext.Recipes
                .Where(r => r.Id == recipe.Id)
                .First();

            // Then average should be 0
            recipeWithRatings.AverageRating.Should().Be(0);
            recipeWithRatings.TotalRatings.Should().Be(0);
        }

        [Fact]
        public void Multiple_Users_Can_Rate_Same_Recipe()
        {
            // Given a popular recipe and multiple users
            var chef = _helper.CreateUser("Chef");
            var recipe = _helper.CreateRecipe(chef, "Amazing Soup");
            var user1 = _helper.CreateUser("User1");
            var user2 = _helper.CreateUser("User2");
            var user3 = _helper.CreateUser("User3");

            _helper.DbContext.Users.AddRange(chef, user1, user2, user3);
            _helper.DbContext.Recipes.Add(recipe);
            _helper.DbContext.SaveChanges();

            // When different users rate it
            var ratings = new[]
            {
                new UserRatingEntity { UserId = user1.Id, RecipeId = recipe.Id, Rating = 5 },
                new UserRatingEntity { UserId = user2.Id, RecipeId = recipe.Id, Rating = 4 },
                new UserRatingEntity { UserId = user3.Id, RecipeId = recipe.Id, Rating = 3 }
            };
            _helper.DbContext.UserRatings.AddRange(ratings);
            _helper.DbContext.SaveChanges();

            // Then average should be calculated correctly
            var ratedRecipe = _helper.DbContext.Recipes
                .Where(r => r.Id == recipe.Id)
                .First();

            ratedRecipe.AverageRating.Should().Be(4); // (5+4+3)/3 = 4
            ratedRecipe.TotalRatings.Should().Be(3);
        }

        public void Dispose() => _helper.Dispose();
    }
}