using System.ComponentModel.DataAnnotations;

namespace MealStack.Web.Models
{
    public class ProfileViewModel
    {
        [Display(Name = "Email")]
        public string Email { get; set; }
        
        [Display(Name = "Username")]
        [Required(ErrorMessage = "Username is required")]
        [StringLength(10, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 10 characters")]
        [RegularExpression(@"^[a-zA-Z]+$", ErrorMessage = "Username can only contain letters (no numbers or special characters)")]
        public string UserName { get; set; }
        
        [Display(Name = "Theme Preference")]
        public string ThemePreference { get; set; } = "light";
    }
}
