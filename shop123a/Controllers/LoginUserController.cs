using Microsoft.AspNetCore.Mvc;
using shop123a.Data;

namespace shop123a.Controllers
{
    public class LoginUserController : Controller
    {
        private readonly Shop123Context db;

        public LoginUserController(Shop123Context context)
        {
            db = context;
        }
        public IActionResult Index()
        {
            return View();
        }
        public IActionResult Register()
        {
            return View();
        }
    }
}