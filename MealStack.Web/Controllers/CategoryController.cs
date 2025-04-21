using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using MealStack.Infrastructure.Data;
using MealStack.Infrastructure.Data.Entities;

namespace MealStack.Web.Controllers
{
    public class CategoryController : BaseController
    {
        private readonly MealStackDbContext _context;

        public CategoryController(
            MealStackDbContext context,
            UserManager<ApplicationUser> userManager)
            : base(userManager)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View();
        }
    }
}