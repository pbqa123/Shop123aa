using System.Text;
using Microsoft.AspNetCore.Mvc;

using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using shop123a.Data;
using shop123a.ViewModels;

namespace shop123a.Controllers
{
    public class AccountController : Controller
    {
        private readonly Shop123Context db;

        public AccountController(Shop123Context context)
        {
            db = context;
        }

        // GET: Register
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // POST: Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                if (db.Customers.Any(c => c.Email == model.Email))
                {
                    ModelState.AddModelError("Email", "Email này đã được sử dụng");
                    return View(model);
                }

                var customer = new Customer
                {
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Email = model.Email,
                    PassWord = HashPassword(model.Password),
                    Phone = model.Phone,
                    Address = model.Address,
                };

                db.Customers.Add(customer);
                await db.SaveChangesAsync();

                HttpContext.Session.SetString("CustomerEmail", customer.Email);
                return RedirectToAction("Index", "Home");
            }
            return View(model);
        }

        // GET: Login
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // POST: Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Kiểm tra admin trước
                if (model.Email == "admin@gmail.com" && model.Password == "123")
                {
                    HttpContext.Session.SetString("IsAdmin", "true");
                    HttpContext.Session.SetString("CustomerEmail", "admin@gmail.com");
                    HttpContext.Session.SetString("CustomerName", "Admin");
                    return RedirectToAction("AdminDashboard", "Admin"); // Thay đổi từ Home sang Admin
                }

                // Kiểm tra login cho customer thông thường
                var customer = db.Customers
                    .FirstOrDefault(c => c.Email == model.Email);

                if (customer != null && VerifyPassword(model.Password, customer.PassWord))
                {
                    HttpContext.Session.SetString("CustomerEmail", customer.Email);
                    HttpContext.Session.SetString("CustomerName", $"{customer.FirstName} {customer.LastName}");
                        HttpContext.Session.SetString("IsAdmin", "false");
                    return RedirectToAction("Index", "Home");
                }

                ModelState.AddModelError("", "Email hoặc mật khẩu không đúng");
            }
            return View(model);
        }

        // Logout
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }

        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }

        private bool VerifyPassword(string enteredPassword, string storedHash)
        {
            return HashPassword(enteredPassword) == storedHash;
        }
    }
}