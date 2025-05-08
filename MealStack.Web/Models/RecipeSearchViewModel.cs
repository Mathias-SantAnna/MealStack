using System.Collections.Generic;

namespace MealStack.Web.Models
{
    public class RecipeSearchViewModel
    {
        // Basic search parameters
        public string SearchTerm { get; set; }
        public string SearchType { get; set; } = "all"; // all, title, ingredients
        public string Difficulty { get; set; }
        public string SortBy { get; set; } = "newest";
        public int? CategoryId { get; set; }
        
        // Advanced search parameters
        public List<string> Ingredients { get; set; } = new List<string>();
        public bool MatchAllIngredients { get; set; } = true;
        public int? MinServings { get; set; }
        public int? MaxServings { get; set; }
        public int? MinPrepTime { get; set; }
        public int? MaxPrepTime { get; set; }
        
        // Helper method to convert ingredients list to comma-separated string
        public string GetIngredientsAsString()
        {
            return Ingredients != null ? string.Join(",", Ingredients) : string.Empty;
        }
        
        // Helper method to parse comma-separated string to ingredients list
        public void SetIngredientsFromString(string ingredientsString)
        {
            if (string.IsNullOrEmpty(ingredientsString))
            {
                Ingredients = new List<string>();
                return;
            }
            
            Ingredients = new List<string>(ingredientsString.Split(','));
        }
    }
}