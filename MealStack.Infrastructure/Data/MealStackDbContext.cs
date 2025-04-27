using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MealStack.Infrastructure.Data.Entities;
using System.Collections.Generic;

namespace MealStack.Infrastructure.Data
{
    public class MealStackDbContext : IdentityDbContext<ApplicationUser>
    {
        public MealStackDbContext(DbContextOptions<MealStackDbContext> options)
            : base(options)
        {
        }
        
        public DbSet<RecipeEntity> Recipes { get; set; }
        public DbSet<IngredientEntity> Ingredients { get; set; }
        public DbSet<CategoryEntity> Categories { get; set; }
        public DbSet<RecipeCategoryEntity> RecipeCategories { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            
            // Recipe creation user relationship
            builder.Entity<RecipeEntity>()
                .HasOne(r => r.CreatedBy)
                .WithMany()
                .HasForeignKey(r => r.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);
                
            // Ingredient creation user relationship
            builder.Entity<IngredientEntity>()
                .HasOne(i => i.CreatedBy)
                .WithMany()
                .HasForeignKey(i => i.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);
                
            // Category creation user relationship
            builder.Entity<CategoryEntity>()
                .HasOne(c => c.CreatedBy)
                .WithMany()
                .HasForeignKey(c => c.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);
                
            // Configure many-to-many relationship
            builder.Entity<RecipeCategoryEntity>()
                .HasKey(rc => new { rc.RecipeId, rc.CategoryId });
                
            builder.Entity<RecipeCategoryEntity>()
                .HasOne(rc => rc.Recipe)
                .WithMany(r => r.RecipeCategories)
                .HasForeignKey(rc => rc.RecipeId)
                .OnDelete(DeleteBehavior.Cascade);
                
            builder.Entity<RecipeCategoryEntity>()
                .HasOne(rc => rc.Category)
                .WithMany(c => c.RecipeCategories)
                .HasForeignKey(rc => rc.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}