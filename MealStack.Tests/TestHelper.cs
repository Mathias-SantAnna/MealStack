using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MealStack.Infrastructure.Data;
using MealStack.Infrastructure.Data.Entities;

namespace MealStack.Tests
{
    public class TestHelper : IDisposable
    {
        public MealStackDbContext DbContext { get; private set; }
        
        public TestHelper()
        {
            var services = new ServiceCollection();
            services.AddDbContext<MealStackDbContext>(options =>
                options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
            
            var serviceProvider = services.BuildServiceProvider();
            DbContext = serviceProvider.GetRequiredService<MealStackDbContext>();
            DbContext.Database.EnsureCreated();
        }
        
        public ApplicationUser CreateUser(string name = "TestUser")
        {
            return new ApplicationUser
            {
                Id = Guid.NewGuid().ToString(),
                UserName = name,
                Email = $"{name.ToLower()}@test.com",
                EmailConfirmed = true
            };
        }
        
        public RecipeEntity CreateRecipe(ApplicationUser user, string title = "Test Recipe")
        {
            return new RecipeEntity
            {
                Title = title,
                Description = "A test recipe",
                Instructions = "1. Mix\n2. Cook\n3. Enjoy",
                Ingredients = "2 cups flour\n1 cup sugar",
                PrepTimeMinutes = 15,
                CookTimeMinutes = 30,
                Servings = 4,
                Difficulty = DifficultyLevel.Easy,
                CreatedById = user.Id,
                CreatedDate = DateTime.UtcNow
            };
        }
        
        public void Dispose() => DbContext?.Dispose();
    }
}