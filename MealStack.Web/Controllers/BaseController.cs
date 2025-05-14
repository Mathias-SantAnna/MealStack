using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using MealStack.Infrastructure.Data.Entities;
using MealStack.Web.Models;
using System;
using System.Diagnostics;
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
                    ViewData["DisplayName"] = user.UserName;
            }
            await next();
        }

        protected RecipeSearchViewModel InitializeSearchModel()
        {
            return new RecipeSearchViewModel
            {
                SearchTerm = Request.Query["searchTerm"],
                SearchType = Request.Query["searchType"],
                Difficulty = Request.Query["difficulty"],
                SortBy = Request.Query["sortBy"].ToString() ?? "newest",
                CategoryId = int.TryParse(Request.Query["categoryId"], out var catId) ? catId : null,
                MinServings = int.TryParse(Request.Query["minServings"], out var minServ) ? minServ : null,
                MaxServings = int.TryParse(Request.Query["maxServings"], out var maxServ) ? maxServ : null,
                MinPrepTime = int.TryParse(Request.Query["minPrepTime"], out var minPrep) ? minPrep : null,
                MaxPrepTime = int.TryParse(Request.Query["maxPrepTime"], out var maxPrep) ? maxPrep : null,
                MatchAllIngredients = Request.Query["matchAllIngredients"] == "true"
            };
        }

        protected IActionResult HandleError(Exception ex, string errorMessage = null, string redirectAction = "Index")
        {
            Debug.WriteLine($"Error: {ex.Message}");
            TempData["Error"] = errorMessage ?? "An error occurred.";
            return RedirectToAction(redirectAction);
        }

        protected async Task<IActionResult> TryExecuteAsync(Func<Task<IActionResult>> action, string errorMessage = null, string redirectAction = "Index")
        {
            try { return await action(); }
            catch (Exception ex) { return HandleError(ex, errorMessage, redirectAction); }
        }
    }
}
