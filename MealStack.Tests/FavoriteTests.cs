using FluentAssertions;
using MealStack.Infrastructure.Data.Entities;

namespace MealStack.Tests
{
    public class FavoriteTests : IDisposable
    {
        private readonly TestHelper _helper = new();

        [Fact]
        public void User_Can_Favorite_A_Recipe()
        {
            // Given a user and a yummy recipe
            var user = _helper.CreateUser("FoodLover");
            var chef = _helper.CreateUser("Chef");
            var recipe = _helper.CreateRecipe(chef, "Amazing Pasta");
            
            // Save users and recipe FIRST (so they get IDs)
            _helper.DbContext.Users.AddRange(user, chef);
            _helper.DbContext.Recipes.Add(recipe);
            _helper.DbContext.SaveChanges(); // This gives them IDs!

            // NOW create favorite with proper IDs
            var favorite = new UserFavoriteEntity
            {
                UserId = user.Id,
                RecipeId = recipe.Id,
                DateAdded = DateTime.UtcNow
            };
            _helper.DbContext.UserFavorites.Add(favorite);
            _helper.DbContext.SaveChanges();

            // Then it should be in their favorites
            var userFavorites = _helper.DbContext.UserFavorites
                .Where(f => f.UserId == user.Id)
                .ToList();
            
            userFavorites.Should().HaveCount(1);
            userFavorites.First().RecipeId.Should().Be(recipe.Id);
        }

        [Fact]
        public void User_Can_Remove_Favorite()
        {
            // Given a user with a favorite recipe
            var user = _helper.CreateUser("ChangingMind");
            var recipe = _helper.CreateRecipe(user, "Okay Recipe");
            
            // Save user and recipe FIRST (so they get IDs)
            _helper.DbContext.Users.Add(user);
            _helper.DbContext.Recipes.Add(recipe);
            _helper.DbContext.SaveChanges(); // This gives them IDs!
            
            // NOW create the favorite with proper IDs
            var favorite = new UserFavoriteEntity
            {
                UserId = user.Id,
                RecipeId = recipe.Id,
                DateAdded = DateTime.UtcNow
            };
            
            _helper.DbContext.UserFavorites.Add(favorite);
            _helper.DbContext.SaveChanges();

            // When they remove it from favorites
            _helper.DbContext.UserFavorites.Remove(favorite);
            _helper.DbContext.SaveChanges();

            // Then their favorites should be empty
            var remainingFavorites = _helper.DbContext.UserFavorites
                .Where(f => f.UserId == user.Id)
                .ToList();
            
            remainingFavorites.Should().BeEmpty();
        }

        [Fact]
        public void User_Cannot_Favorite_Same_Recipe_Twice()
        {
            // Given a user and a recipe they already favorited
            var user = _helper.CreateUser("Enthusiast");
            var recipe = _helper.CreateRecipe(user, "Best Cake Ever");
            
            _helper.DbContext.Users.Add(user);
            _helper.DbContext.Recipes.Add(recipe);
            _helper.DbContext.SaveChanges();

            var firstFavorite = new UserFavoriteEntity
            {
                UserId = user.Id,
                RecipeId = recipe.Id,
                DateAdded = DateTime.UtcNow
            };
            _helper.DbContext.UserFavorites.Add(firstFavorite);
            _helper.DbContext.SaveChanges();

            // When they try to favorite it again
            var secondFavorite = new UserFavoriteEntity
            {
                UserId = user.Id,
                RecipeId = recipe.Id,
                DateAdded = DateTime.UtcNow
            };

            // Then it should cause an error (because of unique constraint)
            Action addDuplicate = () =>
            {
                _helper.DbContext.UserFavorites.Add(secondFavorite);
                _helper.DbContext.SaveChanges();
            };

            addDuplicate.Should().Throw<Exception>();
        }

        [Fact]
        public void User_Can_Have_Multiple_Different_Favorites()
        {
            // Given a user who loves many recipes
            var user = _helper.CreateUser("Foodie");
            var recipe1 = _helper.CreateRecipe(user, "Pizza");
            var recipe2 = _helper.CreateRecipe(user, "Pasta");
            var recipe3 = _helper.CreateRecipe(user, "Salad");

            _helper.DbContext.Users.Add(user);
            _helper.DbContext.Recipes.AddRange(recipe1, recipe2, recipe3);
            _helper.DbContext.SaveChanges();

            // When they favorite all three recipes
            var favorites = new[]
            {
                new UserFavoriteEntity { UserId = user.Id, RecipeId = recipe1.Id, DateAdded = DateTime.UtcNow },
                new UserFavoriteEntity { UserId = user.Id, RecipeId = recipe2.Id, DateAdded = DateTime.UtcNow },
                new UserFavoriteEntity { UserId = user.Id, RecipeId = recipe3.Id, DateAdded = DateTime.UtcNow }
            };
            _helper.DbContext.UserFavorites.AddRange(favorites);
            _helper.DbContext.SaveChanges();

            // Then they should have 3 favorites
            var userFavorites = _helper.DbContext.UserFavorites
                .Where(f => f.UserId == user.Id)
                .ToList();

            userFavorites.Should().HaveCount(3);
        }

        [Fact]
        public void Multiple_Users_Can_Favorite_Same_Recipe()
        {
            // Given a popular recipe and multiple users
            var chef = _helper.CreateUser("Chef");
            var user1 = _helper.CreateUser("User1");
            var user2 = _helper.CreateUser("User2");
            var recipe = _helper.CreateRecipe(chef, "World's Best Pizza");

            _helper.DbContext.Users.AddRange(chef, user1, user2);
            _helper.DbContext.Recipes.Add(recipe);
            _helper.DbContext.SaveChanges();

            // When multiple users favorite the same recipe
            var favorites = new[]
            {
                new UserFavoriteEntity { UserId = user1.Id, RecipeId = recipe.Id, DateAdded = DateTime.UtcNow },
                new UserFavoriteEntity { UserId = user2.Id, RecipeId = recipe.Id, DateAdded = DateTime.UtcNow }
            };
            _helper.DbContext.UserFavorites.AddRange(favorites);
            _helper.DbContext.SaveChanges();

            // Then the recipe should have 2 favorites
            var recipeFavorites = _helper.DbContext.UserFavorites
                .Where(f => f.RecipeId == recipe.Id)
                .ToList();

            recipeFavorites.Should().HaveCount(2);
        }

        public void Dispose() => _helper.Dispose();
    }
}