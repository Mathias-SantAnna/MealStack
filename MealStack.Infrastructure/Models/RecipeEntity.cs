using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MealStack.Infrastructure.Data.Entities
{
    public class RecipeEntity
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Title { get; set; }
        
        [StringLength(500)]
        public string Description { get; set; }
        
        [Required]
        public string Instructions { get; set; }
        
        public string Ingredients { get; set; }
        
        [Range(1, 1440)]
        public int PrepTimeMinutes { get; set; }
        
        [Range(0, 1440)]
        public int CookTimeMinutes { get; set; }
        
        [Range(1, 100)]
        public int Servings { get; set; }
        
        public DifficultyLevel Difficulty { get; set; }
        
        [Required]
        public string CreatedById { get; set; }
        
        [ForeignKey("CreatedById")]
        public virtual ApplicationUser CreatedBy { get; set; }
        
        public DateTime CreatedDate { get; set; }
        
        public DateTime? UpdatedDate { get; set; }
    }
    
    public enum DifficultyLevel
    {
        Easy,
        Medium,
        Hard
    }
}