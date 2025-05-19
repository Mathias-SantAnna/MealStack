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
        public DbSet<UserFavoriteEntity> UserFavorites { get; set; }
        public DbSet<UserRatingEntity> UserRatings { get; set; }
        public DbSet<MealPlanEntity> MealPlans { get; set; }
        public DbSet<MealPlanItemEntity> MealPlanItems { get; set; }
        public DbSet<ShoppingListItemEntity> ShoppingListItems { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            
            builder.Entity<RecipeEntity>()
                .HasOne(r => r.CreatedBy)
                .WithMany()
                .HasForeignKey(r => r.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);
            
            builder.Entity<IngredientEntity>()
                .HasOne(i => i.CreatedBy)
                .WithMany()
                .HasForeignKey(i => i.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);
                
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
            
            builder.Entity<UserFavoriteEntity>()
                .HasKey(uf => new { uf.UserId, uf.RecipeId });
                
            builder.Entity<UserFavoriteEntity>()
                .HasOne(uf => uf.User)
                .WithMany()
                .HasForeignKey(uf => uf.UserId)
                .OnDelete(DeleteBehavior.Cascade);
                
            builder.Entity<UserFavoriteEntity>()
                .HasOne(uf => uf.Recipe)
                .WithMany()
                .HasForeignKey(uf => uf.RecipeId)
                .OnDelete(DeleteBehavior.Cascade);
            
            builder.Entity<UserRatingEntity>()
                .HasKey(ur => new { ur.UserId, ur.RecipeId });

            builder.Entity<UserRatingEntity>()
                .HasOne(ur => ur.User)
                .WithMany()
                .HasForeignKey(ur => ur.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<UserRatingEntity>()
                .HasOne(ur => ur.Recipe)
                .WithMany(r => r.Ratings)
                .HasForeignKey(ur => ur.RecipeId)
                .OnDelete(DeleteBehavior.Cascade);
                
            builder.Entity<MealPlanEntity>()
                .HasOne(m => m.User)
                .WithMany()
                .HasForeignKey(m => m.UserId)
                .OnDelete(DeleteBehavior.Cascade);
                
            builder.Entity<MealPlanItemEntity>()
                .HasOne(mi => mi.MealPlan)
                .WithMany(m => m.MealItems)
                .HasForeignKey(mi => mi.MealPlanId)
                .OnDelete(DeleteBehavior.Cascade);
                
            builder.Entity<MealPlanItemEntity>()
                .HasOne(mi => mi.Recipe)
                .WithMany()
                .HasForeignKey(mi => mi.RecipeId)
                .OnDelete(DeleteBehavior.Restrict);
                
            builder.Entity<ShoppingListItemEntity>()
                .HasOne(si => si.MealPlan)
                .WithMany(m => m.ShoppingItems)
                .HasForeignKey(si => si.MealPlanId)
                .OnDelete(DeleteBehavior.Cascade);
                
            builder.Entity<ShoppingListItemEntity>()
                .HasOne(si => si.Ingredient)
                .WithMany()
                .HasForeignKey(si => si.IngredientId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}