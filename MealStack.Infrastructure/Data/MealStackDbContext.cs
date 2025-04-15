using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MealStack.Infrastructure.Data.Entities;

namespace MealStack.Infrastructure.Data
{
    public class MealStackDbContext : IdentityDbContext<IdentityUser>
    {
        public MealStackDbContext(DbContextOptions<MealStackDbContext> options)
            : base(options)
        {
        }
        
        // Make sure this property exists and is public
        public DbSet<RecipeEntity> Recipes { get; set; }
        
        // Add this property for Ingredients
        public DbSet<IngredientEntity> Ingredients { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            
            // Make sure the relationship is properly configured
            builder.Entity<RecipeEntity>()
                .HasOne(r => r.CreatedBy)
                .WithMany()
                .HasForeignKey(r => r.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);
                
            // Add this new configuration for IngredientEntity
            builder.Entity<IngredientEntity>()
                .HasOne(i => i.CreatedBy)
                .WithMany()
                .HasForeignKey(i => i.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}