using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MealStack.Infrastructure.Data.Entities;

namespace MealStack.Infrastructure.Data
{
    public class MealPlanEntity
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Name { get; set; }
        
        [StringLength(500)]
        public string Description { get; set; }
        
        [Required]
        public DateTime StartDate { get; set; }
        
        [Required]
        public DateTime EndDate { get; set; }
        
        [Required]
        public string UserId { get; set; }
        
        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; }
        
        public DateTime CreatedDate { get; set; }
        
        public virtual ICollection<MealPlanItemEntity> MealItems { get; set; } = new List<MealPlanItemEntity>();
        
        public virtual ICollection<ShoppingListItemEntity> ShoppingItems { get; set; } = new List<ShoppingListItemEntity>();
    }
}