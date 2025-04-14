using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MealStack.Infrastructure.Data.Entities;

namespace MealStack.Infrastructure.Data;

public class MealStackDbContext : IdentityDbContext<IdentityUser>
{
    public MealStackDbContext(DbContextOptions<MealStackDbContext> options)
        : base(options)
    {
    }
    
    public DbSet<RecipeEntity> Recipes { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        
        builder.Entity<RecipeEntity>()
            .HasOne(r => r.CreatedBy)
            .WithMany()
            .HasForeignKey(r => r.CreatedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}