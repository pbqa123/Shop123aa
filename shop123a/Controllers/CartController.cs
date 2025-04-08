using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using shop123a.Data;
using shop123a.ViewModels;

namespace Shopeco.Controllers
{
    public class CartController : Controller
    {
        private readonly Shop123Context db;

        public CartController(Shop123Context context)
        {
            db = context;
        }
        public List<CartItem> Cart => HttpContext.Session.Get<List<CartItem>>(MySetting.CART_KEY) ?? new List<CartItem>();
        public IActionResult Index()
        {
            return View(Cart);
        }
        public IActionResult AddToCart(int ProductId, int quantity = 1)
        {
            var giohang = Cart;
            var item = giohang.SingleOrDefault(p => p.ProductId == ProductId);
            if (item == null)
            {
                var hangHoa = db.Products.SingleOrDefault(p => p.ProductId == ProductId);
                if (hangHoa == null)
                {
                    TempData["Message"] = $"Do not search";
                    return Redirect("/404");
                }
                item = new CartItem
                {
                    ProductId = hangHoa.ProductId,
                    ProductName = hangHoa.ProductName,
                    Price = hangHoa.Price,
                    ImageUrl = hangHoa.ImageUrl,
                    SoLuong = quantity
                };
                giohang.Add(item);
            }
            else
            {
                item.SoLuong += quantity;
            }
            HttpContext.Session.Set(MySetting.CART_KEY, giohang);
            return RedirectToAction("Index");
        }
        public IActionResult RemoveCart(int id)
        {
            var giohang = Cart; // Giả sử Cart là một danh sách được lấy từ session hoặc đâu đó
            var item = giohang.SingleOrDefault(p => p.ProductId == id);
            if (item != null) // Nếu tìm thấy item
            {
                giohang.Remove(item); // Xóa item khỏi giỏ hàng
                HttpContext.Session.Set(MySetting.CART_KEY, giohang); // Cập nhật lại session
            }
            return RedirectToAction("Index");
        }
        [HttpGet]
        public IActionResult Checkout()
        {
            // Kiểm tra Cart có tồn tại và không trống
            if (Cart == null || Cart.Count == 0)
            {
                return Redirect("/");
            }

            // Trả về view với dữ liệu Cart
            return View(Cart);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Checkout(CheckoutVM model)
        {
            if (!ModelState.IsValid)
            {
                return View(Cart); // Trả lại view nếu dữ liệu không hợp lệ
            }

            if (Cart == null || Cart.Count == 0)
            {
                TempData["Message"] = "Giỏ hàng trống!";
                return RedirectToAction("Index");
            }

            // Tạo mới Order
            var order = new Order
            {
                OrderDate = DateTime.Now,
                TotalAmount = Cart.Sum(item => item.Price * item.SoLuong),
                Status = "Pending", // Trạng thái ban đầu
                NameUser = model.Name,
                Address = model.Address,
                Phone = model.phone
                // CustomerId có thể để null nếu không yêu cầu đăng nhập
            };

            // Thêm Order vào database
            db.Orders.Add(order);
            db.SaveChanges();

            // Xóa giỏ hàng sau khi checkout thành công
            HttpContext.Session.Remove(MySetting.CART_KEY);

            TempData["Message"] = "Đặt hàng thành công!";
            return RedirectToAction("OrderSuccess");
        }

        public IActionResult OrderSuccess()
        {
            return View();
        }
    }

}
