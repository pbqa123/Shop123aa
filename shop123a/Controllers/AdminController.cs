using Microsoft.AspNetCore.Mvc;

namespace shop123a.Controllers
{
    public class AdminController : Controller
    {
        public IActionResult AdminDashboard()
        {
            if (HttpContext.Session.GetString("IsAdmin") != "true")
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }
    }
}