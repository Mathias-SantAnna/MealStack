using System;

namespace MealStack.Domain.Models
{
    public class Recipe
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Ingredients { get; set; }
        public string Instructions { get; set; }
        public int PrepTimeMinutes { get; set; }
        public int CookTimeMinutes { get; set; }
        public DifficultyLevel Difficulty { get; set; }
        public int Servings { get; set; }
        
        // Add this new property
        public string ImagePath { get; set; }
        
        public string CreatedById { get; set; }
        public string CreatedByName { get; set; }
        
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }
    
    public enum DifficultyLevel
    {
        Easy,
        Medium,
        Masterchef
    }
}