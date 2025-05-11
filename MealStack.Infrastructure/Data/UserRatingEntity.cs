using System;
using MealStack.Infrastructure.Data.Entities;

public class UserRatingEntity
{
    public string UserId { get; set; }
    public int RecipeId { get; set; }
    public int Rating { get; set; } // 1-5 stars
    public DateTime DateRated { get; set; }
    
    public virtual ApplicationUser User { get; set; }
    public virtual RecipeEntity Recipe { get; set; }
}