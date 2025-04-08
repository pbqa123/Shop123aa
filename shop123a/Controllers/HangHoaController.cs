using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using shop123a.Data;
using shop123a.ViewModels;


namespace shop123a.Controllers
{
    public class HangHoaController : Controller
    {
        private readonly Shop123Context db;

        public HangHoaController(Shop123Context context)
        {
            db = context;
        }
        public IActionResult Index(int? Category)
        {
            var Products = db.Products.AsQueryable();
            if (Category.HasValue)
            {
                Products = Products.Where(p => p.CategoryId == Category.Value);
            }
            var result = Products.Select(p => new ProductVM
            {
                ProductId = p.ProductId,
                ProductName = p.ProductName,
                Description = p.Description,
                Price = p.Price,
                ImageUrl = p.ImageUrl ?? "",
            });
            return View(result);
        }
        public IActionResult Detail(int? Id)
        {
            var data = db.Products
                .SingleOrDefault(p => p.ProductId == Id);
            if (data == null)
            {
                TempData["Message"] = "Cannotitem";
                return Redirect("/404");
            }

            var result = new ChiTietHH
            {
                ProductId = data.ProductId,
                ProductName = data.ProductName,
                Description = data.Description,
                Price = data.Price,
                ImageUrl = data.ImageUrl ?? "",
            };
            return View(result);
        }
    }
}

