using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MealStack.Infrastructure.Data.Entities;

namespace MealStack.Infrastructure.Data
{
    public class MealPlanItemEntity
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int MealPlanId { get; set; }
        
        [ForeignKey("MealPlanId")]
        public virtual MealPlanEntity MealPlan { get; set; }
        
        [Required]
        public int RecipeId { get; set; }
        
        [ForeignKey("RecipeId")]
        public virtual RecipeEntity Recipe { get; set; }
        
        [Required]
        public DateTime PlannedDate { get; set; }
        
        [Required]
        public MealType MealType { get; set; }
        
        [Required]
        [Range(1, 20)]
        public int Servings { get; set; }
        
        public string Notes { get; set; }
    }
    
    public enum MealType
    {
        Breakfast,
        Lunch,
        Dinner,
        Snack,
        Other
    }
}