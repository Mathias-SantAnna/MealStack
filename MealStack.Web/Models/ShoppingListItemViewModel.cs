using System.ComponentModel.DataAnnotations;

namespace MealStack.Web.Models
{
    public class ShoppingListItemViewModel
    {
        public int Id { get; set; }
        public int MealPlanId { get; set; }

        [Required]
        [Display(Name = "Ingredient")]
        public string IngredientName { get; set; }


        public string Quantity { get; set; }
        public string Unit { get; set; }
        public string Category { get; set; }
        public bool IsChecked { get; set; }
        public int? IngredientId { get; set; } 

 
        public string DisplayAmount
        {
            get
            {
                if (string.IsNullOrEmpty(Quantity) && string.IsNullOrEmpty(Unit)) return string.Empty;
                if (string.IsNullOrEmpty(Unit)) return Quantity?.Trim() ?? string.Empty;
                return $"{Quantity?.Trim()} {Unit.Trim()}".Trim();
            }
        }
    }
}