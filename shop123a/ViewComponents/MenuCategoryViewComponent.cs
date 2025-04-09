using Microsoft.AspNetCore.Mvc;
using shop123a.Data;
using shop123a.ViewModels;

namespace shop123a.ViewComponents
{
    public class MenuCategoryViewComponent : ViewComponent
    {
        private readonly Shop123Context db;

        public MenuCategoryViewComponent(Shop123Context context) => db = context;
        public IViewComponentResult Invoke()
        {
            var data = db.Categories.Select(lo => new MenuCategoryVM
            {
                CategoryId = lo.CategoryId,
                CategoryName = lo.CategoryName
            });
            return View(data);
        }

    }
}
