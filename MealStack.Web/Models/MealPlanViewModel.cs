using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace MealStack.Web.Models
{
    public class MealPlanViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Name is required")]
        [StringLength(100, ErrorMessage = "Name cannot be longer than 100 characters")]
        public string Name { get; set; }

        [StringLength(500, ErrorMessage = "Description cannot be longer than 500 characters")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Start date is required")]
        [DataType(DataType.Date)]
        [Display(Name = "Start Date")]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "End date is required")]
        [DataType(DataType.Date)]
        [Display(Name = "End Date")]
        public DateTime EndDate { get; set; }

        public string UserId { get; set; } 
        public DateTime CreatedDate { get; set; }
        
        public List<MealPlanItemViewModel> MealItems { get; set; } = new List<MealPlanItemViewModel>();
        public List<ShoppingListItemViewModel> ShoppingItems { get; set; } = new List<ShoppingListItemViewModel>();
        
        public int MealItemsCount { get; set; }
        public int ShoppingItemsCount { get; set; } 
        
        public string DateRange => $"{StartDate:MMM d} - {EndDate:MMM d, yyyy}";
        public int Days => (EndDate - StartDate).Days + 1;
        public int TotalMeals => MealItems?.Count ?? 0;
    }
}