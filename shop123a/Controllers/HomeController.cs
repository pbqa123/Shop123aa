using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using shop123a.Data;
using shop123a.Models;
using shop123a.ViewModels;

namespace Shopeco.Controllers;

public class HomeController : Controller
{
    public HomeController(Shop123Context context)
    {
        db = context;
    }
    private readonly Shop123Context db;

    public IActionResult Index()
    {
        var Products = db.Products.AsQueryable();

        var result = Products.Select(p => new ProductVM
        {
            ProductId = p.ProductId,
            ProductName = p.ProductName,
            Description = p.Description,
            Price = p.Price,
            CategoryId = p.CategoryId,
            ImageUrl = p.ImageUrl ?? "",
        });
        return View(result);
    }
    [Route("/404")]
    public IActionResult PageNotFound()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

}
