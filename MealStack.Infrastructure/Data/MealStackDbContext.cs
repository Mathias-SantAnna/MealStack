using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace MealStack.Infrastructure.Data
{
    public class MealStackDbContext : IdentityDbContext<IdentityUser>
    {
        public MealStackDbContext(DbContextOptions<MealStackDbContext> options)
            : base(options)
        {
        }

        // TODO: Add DbSets later
    }
}