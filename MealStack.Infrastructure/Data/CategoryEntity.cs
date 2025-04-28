using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MealStack.Infrastructure.Data.Entities
{
    public class CategoryEntity
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(50)]
        public string Name { get; set; }
        
        public string Description { get; set; }
        
        // Changed from Color to ColorClass
        public string ColorClass { get; set; }
        
        public DateTime CreatedDate { get; set; }
        
        public string CreatedById { get; set; }
        
        [ForeignKey("CreatedById")]
        public ApplicationUser CreatedBy { get; set; }
        
        public virtual ICollection<RecipeCategoryEntity> RecipeCategories { get; set; } = new List<RecipeCategoryEntity>();
    }
}