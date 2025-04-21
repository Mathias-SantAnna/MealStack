using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using MealStack.Infrastructure.Data.Entities;
using System.Threading.Tasks;

namespace MealStack.Web.Controllers
{
    public abstract class BaseController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public BaseController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            
            if (User.Identity.IsAuthenticated)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null && !string.IsNullOrEmpty(user.UserName))
                {
                    ViewData["DisplayName"] = user.UserName; 
                }
            }

            await next();
        }
    }
}