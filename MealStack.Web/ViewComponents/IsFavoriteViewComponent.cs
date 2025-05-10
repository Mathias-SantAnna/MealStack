using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MealStack.Infrastructure.Data;
using MealStack.Infrastructure.Data.Entities;
using System.Threading.Tasks;

namespace MealStack.Web.ViewComponents
{
    public class IsFavoriteViewComponent : ViewComponent
    {
        private readonly MealStackDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public IsFavoriteViewComponent(MealStackDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IViewComponentResult> InvokeAsync(int recipeId)
        {
            if (!User.Identity.IsAuthenticated)
                return View(false);
                
            var userId = _userManager.GetUserId(HttpContext.User);
            bool isFavorite = await _context.UserFavorites
                .AnyAsync(uf => uf.UserId == userId && uf.RecipeId == recipeId);
                
            return View(isFavorite);
        }
    }
}