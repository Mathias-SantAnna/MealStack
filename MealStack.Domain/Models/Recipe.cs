using System;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

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
        
        public string CreatedById { get; set; }
        public IdentityUser CreatedBy { get; set; }
        
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