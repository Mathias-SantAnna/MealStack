using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using MealStack.Infrastructure.Data.Entities;
using MealStack.Web.Models;
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

        protected RecipeSearchViewModel InitializeSearchModel()
        {
            var searchModel = new RecipeSearchViewModel
            {
                SearchTerm = Request.Query["searchTerm"],
                SearchType = Request.Query["searchType"],
                Difficulty = Request.Query["difficulty"],
                SortBy = Request.Query["sortBy"].ToString() ?? "newest",
                CategoryId = !string.IsNullOrEmpty(Request.Query["categoryId"]) ?
                    int.Parse(Request.Query["categoryId"]) : null,
                MinServings = !string.IsNullOrEmpty(Request.Query["minServings"]) ?
                    int.Parse(Request.Query["minServings"]) : null,
                MaxServings = !string.IsNullOrEmpty(Request.Query["maxServings"]) ?
                    int.Parse(Request.Query["maxServings"]) : null,
                MinPrepTime = !string.IsNullOrEmpty(Request.Query["minPrepTime"]) ?
                    int.Parse(Request.Query["minPrepTime"]) : null,
                MaxPrepTime = !string.IsNullOrEmpty(Request.Query["maxPrepTime"]) ?
                    int.Parse(Request.Query["maxPrepTime"]) : null,
                MatchAllIngredients = Request.Query["matchAllIngredients"] == "true"
            };
            
            if (!string.IsNullOrEmpty(Request.Query["ingredients"]))
            {
                searchModel.SetIngredientsFromString(Request.Query["ingredients"]);
            }
            
            return searchModel;
        }

        protected void SetSearchViewData(RecipeSearchViewModel searchModel, string actionName)
        {
            ViewData["SearchTerm"] = searchModel.SearchTerm;
            ViewData["SearchType"] = searchModel.SearchType ?? "all";
            ViewData["Difficulty"] = searchModel.Difficulty;
            ViewData["SortBy"] = searchModel.SortBy ?? "newest";
            ViewData["MatchAllIngredients"] = searchModel.MatchAllIngredients.ToString().ToLower();
            ViewData["CategoryId"] = searchModel.CategoryId;
            ViewData["SearchAction"] = actionName;
        }
    }
}