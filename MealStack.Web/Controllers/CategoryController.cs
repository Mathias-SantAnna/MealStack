using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MealStack.Infrastructure.Data;

namespace MealStack.Web.Controllers
{
    public class CategoryController : Controller
    {
        private readonly MealStackDbContext _context;

        public CategoryController(MealStackDbContext context)
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