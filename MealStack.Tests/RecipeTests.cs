using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace MealStack.Tests
{
    public class RecipeTests : IDisposable
    {
        private readonly TestHelper _helper = new();
        
        [Fact]
        public void CanCreateRecipe()
        {
            // Given a user
            var user = _helper.CreateUser();
            var recipe = _helper.CreateRecipe(user, "Chocolate Cake");
            
            // When saving recipe
            _helper.DbContext.Recipes.Add(recipe);
            _helper.DbContext.SaveChanges();
            
            // Then recipe exists
            var saved = _helper.DbContext.Recipes.First();
            saved.Title.Should().Be("Chocolate Cake");
            saved.CreatedById.Should().Be(user.Id);
        }
        
        [Fact]
        public void CanSearchRecipes()
        {
            // Given recipes with different titles
            var user = _helper.CreateUser();
            _helper.DbContext.Recipes.AddRange(
                _helper.CreateRecipe(user, "Chocolate Cake"),
                _helper.CreateRecipe(user, "Vanilla Cake"),
                _helper.CreateRecipe(user, "Apple Pie")
            );
            _helper.DbContext.SaveChanges();
            
            // When searching for "cake"
            var results = _helper.DbContext.Recipes
                .Where(r => r.Title.Contains("Cake"))
                .ToList();
            
            // Then find cake recipes only
            results.Should().HaveCount(2);
            results.Should().OnlyContain(r => r.Title.Contains("Cake"));
        }
        
        [Fact]
        public void RecipeCalculatesAverageRating()
        {
            // Given a recipe with ratings
            var user = _helper.CreateUser();
            var recipe = _helper.CreateRecipe(user);
            _helper.DbContext.Recipes.Add(recipe);
            _helper.DbContext.SaveChanges();
            
            _helper.DbContext.UserRatings.AddRange(
                new UserRatingEntity { RecipeId = recipe.Id, UserId = "user1", Rating = 5 },
                new UserRatingEntity { RecipeId = recipe.Id, UserId = "user2", Rating = 3 }
            );
            _helper.DbContext.SaveChanges();
            
            // When getting recipe with ratings
            var recipeWithRatings = _helper.DbContext.Recipes
                .Include(r => r.Ratings)
                .First(r => r.Id == recipe.Id);
            
            // Then average is correct
            recipeWithRatings.AverageRating.Should().Be(4); // (5+3)/2 = 4
        }
        
        public void Dispose() => _helper.Dispose();
    }
}