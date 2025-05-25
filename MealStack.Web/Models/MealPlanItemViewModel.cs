using System;
using System.ComponentModel.DataAnnotations;
using MealStack.Infrastructure.Data; 

namespace MealStack.Web.Models
{
    public class MealPlanItemViewModel
    {
        public int Id { get; set; }
        
        public string UserId { get; set; } 

        [Required]
        public int MealPlanId { get; set; }

        [Required]
        public int RecipeId { get; set; }
        
        public string RecipeTitle { get; set; }
        public string RecipeImagePath { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Date")]
        public DateTime PlannedDate { get; set; }

        [Required]
        [Display(Name = "Meal")]
        public MealType MealType { get; set; }

        [Required]
        [Range(1, 20, ErrorMessage = "Servings must be between 1 and 20")]
        [Display(Name = "Servings")]
        public int Servings { get; set; }
        
        [Display(Name = "Notes")]
        public string Notes { get; set; } = string.Empty;
        
        public string MealTypeDisplay => MealType.ToString();
        public string DateDisplay => PlannedDate.ToString("dd/MM/yyyy");
        public string DayOfWeek => PlannedDate.DayOfWeek.ToString();
        
        public string GetMealTypeClass()
        {
            return MealType switch
            {
                MealType.Breakfast => "bg-warning text-dark",
                MealType.Lunch => "bg-info text-dark",
                MealType.Dinner => "bg-primary text-white",
                MealType.Snack => "bg-success text-white",
                _ => "bg-secondary text-white"
            };
        }

        public string GetMealTypeIcon()
        {
            return MealType switch
            {
                MealType.Breakfast => "bi-sunrise",
                MealType.Lunch => "bi-sun",
                MealType.Dinner => "bi-moon-stars",
                MealType.Snack => "bi-apple",
                _ => "bi-cup"
            };
        }
    }
}