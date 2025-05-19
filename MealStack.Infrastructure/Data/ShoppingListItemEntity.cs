using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MealStack.Infrastructure.Data
{
    public class ShoppingListItemEntity
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int MealPlanId { get; set; }
        
        [ForeignKey("MealPlanId")]
        public virtual MealPlanEntity MealPlan { get; set; }
        
        [Required]
        [StringLength(200)]
        public string IngredientName { get; set; }
        
        [Required]
        public string Quantity { get; set; }
        
        public string Unit { get; set; }
        
        public bool IsChecked { get; set; }
        
        public int? IngredientId { get; set; }
        
        [ForeignKey("IngredientId")]
        public virtual Entities.IngredientEntity Ingredient { get; set; }
        
        public string Category { get; set; }
    }
}