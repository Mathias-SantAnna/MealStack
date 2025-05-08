using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MealStack.Infrastructure.Data.Entities
{
    public class IngredientEntity
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Name { get; set; }
        
        public string Description { get; set; }
        
        [StringLength(50)]
        public string Category { get; set; }
        
        public string Measurement { get; set; }
        
        public DateTime CreatedDate { get; set; }
        
        public string CreatedById { get; set; }
        
        [ForeignKey("CreatedById")]
        public ApplicationUser CreatedBy { get; set; }
    }
}