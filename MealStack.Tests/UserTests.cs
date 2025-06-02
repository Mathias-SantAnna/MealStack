using FluentAssertions;

namespace MealStack.Tests
{
    public class UserTests : IDisposable
    {
        private readonly TestHelper _helper = new();

        [Fact]
        public void User_Can_Be_Created()
        {
            // I want to create a user
            var user = _helper.CreateUser("TestChef");

            // The user should have the right name and email
            user.UserName.Should().Be("TestChef");
            user.Email.Should().Be("testchef@test.com");
            user.EmailConfirmed.Should().BeTrue();
        }

        [Fact]
        public void User_Can_Have_Multiple_Recipes()
        {
            // Given a user who loves cooking
            var user = _helper.CreateUser("CookingMama");
            _helper.DbContext.Users.Add(user);
            _helper.DbContext.SaveChanges();

            // When they create multiple recipes
            var recipes = new[]
            {
                _helper.CreateRecipe(user, "Pancakes"),
                _helper.CreateRecipe(user, "Waffles"),
                _helper.CreateRecipe(user, "French Toast")
            };
            _helper.DbContext.Recipes.AddRange(recipes);
            _helper.DbContext.SaveChanges();

            // Then they should have 3 recipes
            var userRecipes = _helper.DbContext.Recipes
                .Where(r => r.CreatedById == user.Id)
                .ToList();
            
            userRecipes.Should().HaveCount(3);
        }

        public void Dispose() => _helper.Dispose();
    }
}