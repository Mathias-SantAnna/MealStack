using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace MealStack.Infrastructure.Data
{
    public class MealStackDbContext : IdentityDbContext<IdentityUser>
    {
        public MealStackDbContext(DbContextOptions<MealStackDbContext> options)
            : base(options) { }
    }
}