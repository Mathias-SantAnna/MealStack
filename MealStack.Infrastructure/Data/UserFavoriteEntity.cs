using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MealStack.Infrastructure.Data.Entities
{
    public class UserFavoriteEntity
    {
        // Composite primary key
        [Key, Column(Order = 0)]
        public string UserId { get; set; }
        
        [Key, Column(Order = 1)]
        public int RecipeId { get; set; }
        
        public DateTime DateAdded { get; set; }
        
        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; }
        
        [ForeignKey("RecipeId")]
        public virtual RecipeEntity Recipe { get; set; }
    }
}