using FluentAssertions;
using MealStack.Infrastructure.Data.Entities;

namespace MealStack.Tests
{
    public class CategoryTests : IDisposable
    {
        private readonly TestHelper _helper = new();

        [Fact]
        public void Recipe_Can_Have_Categories()
        {
            // Given a recipe and some categories
            var user = _helper.CreateUser("Categorizer");
            var recipe = _helper.CreateRecipe(user, "Chocolate Cake");
            
            var dessertCategory = new CategoryEntity
            {
                Name = "Desserts",
                Description = "Sweet treats",
                ColorClass = "warning"
            };
            
            var quickCategory = new CategoryEntity
            {
                Name = "Quick",
                Description = "Fast to make",
                ColorClass = "success"
            };

            _helper.DbContext.Users.Add(user);
            _helper.DbContext.Recipes.Add(recipe);
            _helper.DbContext.Categories.AddRange(dessertCategory, quickCategory);
            _helper.DbContext.SaveChanges();

            // When adding categories to recipe
            var recipeCategories = new[]
            {
                new RecipeCategoryEntity { RecipeId = recipe.Id, CategoryId = dessertCategory.Id },
                new RecipeCategoryEntity { RecipeId = recipe.Id, CategoryId = quickCategory.Id }
            };
            _helper.DbContext.RecipeCategories.AddRange(recipeCategories);
            _helper.DbContext.SaveChanges();

            // Then recipe should have 2 categories
            var recipeCats = _helper.DbContext.RecipeCategories
                .Where(rc => rc.RecipeId == recipe.Id)
                .ToList();
            
            recipeCats.Should().HaveCount(2);
        }

        [Fact]
        public void Can_Find_Recipes_By_Category()
        {
            // Given recipes in different categories
            var user = _helper.CreateUser("Chef");
            var italianCategory = new CategoryEntity { Name = "Italian", ColorClass = "info" };
            
            var pastaRecipe = _helper.CreateRecipe(user, "Spaghetti");
            var pizzaRecipe = _helper.CreateRecipe(user, "Margherita Pizza");
            var saladRecipe = _helper.CreateRecipe(user, "Greek Salad");

            _helper.DbContext.Users.Add(user);
            _helper.DbContext.Categories.Add(italianCategory);
            _helper.DbContext.Recipes.AddRange(pastaRecipe, pizzaRecipe, saladRecipe);
            _helper.DbContext.SaveChanges();

            // Only pasta and pizza are Italian
            _helper.DbContext.RecipeCategories.AddRange(
                new RecipeCategoryEntity { RecipeId = pastaRecipe.Id, CategoryId = italianCategory.Id },
                new RecipeCategoryEntity { RecipeId = pizzaRecipe.Id, CategoryId = italianCategory.Id }
            );
            _helper.DbContext.SaveChanges();

            // When searching for Italian recipes
            var italianRecipes = _helper.DbContext.RecipeCategories
                .Where(rc => rc.CategoryId == italianCategory.Id)
                .Select(rc => rc.Recipe)
                .ToList();

            // Then should find 2 Italian recipes
            italianRecipes.Should().HaveCount(2);
            italianRecipes.Should().Contain(r => r.Title == "Spaghetti");
            italianRecipes.Should().Contain(r => r.Title == "Margherita Pizza");
        }

        public void Dispose() => _helper.Dispose();
    }
}
