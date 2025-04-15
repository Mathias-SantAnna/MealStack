using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace MealStack.Infrastructure.Data.Entities
{
    public class IngredientEntity
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Name { get; set; }
        
        public string Description { get; set; }
        
        [StringLength(50)]
        public string Category { get; set; }
        
        public string Measurement { get; set; } // e.g., "cups", "tablespoons", "grams"
        
        public DateTime CreatedDate { get; set; }
        
        public string CreatedById { get; set; }
        public IdentityUser CreatedBy { get; set; }
    }
}