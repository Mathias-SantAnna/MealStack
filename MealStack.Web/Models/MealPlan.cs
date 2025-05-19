namespace MealStack.Domain.Models
{
    public class MealPlan
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string UserId { get; set; }
        public DateTime CreatedDate { get; set; }
        public ICollection<MealPlanItem> Items { get; set; } = new List<MealPlanItem>();
    }
}

// Individual meal items in a plan
namespace MealStack.Domain.Models
{
    public class MealPlanItem
    {
        public int Id { get; set; }
        public int MealPlanId { get; set; }
        public MealPlan MealPlan { get; set; }
        public int RecipeId { get; set; }
        public Recipe Recipe { get; set; }
        public DateTime PlannedDate { get; set; }
        public MealType MealType { get; set; }
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

// Aggregated ingredients for shopping
namespace MealStack.Domain.Models
{
    public class ShoppingListItem
    {
        public int Id { get; set; }
        public int MealPlanId { get; set; }
        public MealPlan MealPlan { get; set; }
        public string IngredientName { get; set; }
        public string Quantity { get; set; }
        public string Unit { get; set; }
        public bool IsChecked { get; set; }
        public int? OriginalIngredientId { get; set; }
    }
}