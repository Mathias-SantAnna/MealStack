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
        public string Notes { get; set; }
        
        public string MealTypeDisplay => MealType.ToString();
        public string DateDisplay => PlannedDate.ToString("MMM d, yyyy");
        public string DayOfWeek => PlannedDate.DayOfWeek.ToString();
    }
}