using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MealStack.Infrastructure.Data.Entities
{
    public class RecipeCategoryEntity
    {
        public int RecipeId { get; set; }
        
        [ForeignKey("RecipeId")]
        public RecipeEntity Recipe { get; set; }
        
        public int CategoryId { get; set; }
        
        [ForeignKey("CategoryId")]
        public CategoryEntity Category { get; set; }
    }
}