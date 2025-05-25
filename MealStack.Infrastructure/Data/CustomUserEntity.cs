using Microsoft.AspNetCore.Identity;

namespace MealStack.Infrastructure.Data.Entities
{
    public class ApplicationUser : IdentityUser
    {
        public string ThemePreference { get; set; } = "light";
    }
}