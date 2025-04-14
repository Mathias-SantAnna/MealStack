using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace MealStack.Infrastructure.Data.Entities
{
    public enum DifficultyLevel
    {
        Easy,
        Medium,
        Hard
    }

    public class RecipeEntity
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Title { get; set; }
        
        [StringLength(500)]
        public string Description { get; set; }
        
        [Required]
        public string Instructions { get; set; }
        
        [Range(1, 1440)]
        public int PrepTimeMinutes { get; set; }
        
        [Range(0, 1440)]
        public int CookTimeMinutes { get; set; }
        
        [Range(1, 100)]
        public int Servings { get; set; }
        
        public DifficultyLevel Difficulty { get; set; }
        
        public string Ingredients { get; set; }
        
        [Required]
        public DateTime CreatedDate { get; set; }
        
        public DateTime? UpdatedDate { get; set; }
        
        [Required]
        public string CreatedById { get; set; }
        
        public virtual IdentityUser CreatedBy { get; set; }
    }
}