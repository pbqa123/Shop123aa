using System.Text;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using shop123a.Data;
using System.Net.Http;
using System.Threading.Tasks;
using System.Linq;

namespace shop123a.Controllers
{
    public class AiController : Controller
    {
        private readonly Shop123Context db;
        private static string _lastResponse = null;

        public AiController(Shop123Context context)
        {
            db = context;
        }

        [HttpPost]
        public async Task<JsonResult> GetChatbotResponse(string message, int? productId = null)
        {
            if (productId.HasValue && string.IsNullOrEmpty(message)) // Khi nhấn "Hỏi AI" lần đầu
            {
                var product = db.Products.FirstOrDefault(p => p.ProductId == productId);
                if (product != null)
                {
                    string initialResponse = $"Chào bạn! Đây là thông tin về {product.ProductName} mà mình đang có nha:\n" +
                                            $"- Giá chỉ: ${product.Price} - quá hời luôn!\n" +
                                            $"- Mô tả: {product.Description}\n" +
                                            $"- Còn {product.QuantityInStock} cái trong kho thôi, nhanh tay kẻo hết nha!\n" +
                                            $"Bạn muốn biết thêm gì không? Hãng nào, năm sản xuất, hay cách dùng? Cứ hỏi mình, mình tư vấn nhiệt tình luôn!";
                    _lastResponse = initialResponse;
                    return Json(new { reply = initialResponse });
                }
                return Json(new { reply = "Ôi, không tìm thấy sản phẩm này rồi! Bạn thử kiểm tra lại xem sao nha!" });
            }
            else if (productId.HasValue && !string.IsNullOrEmpty(message)) // Khi người dùng hỏi thêm
            {
                var product = db.Products.FirstOrDefault(p => p.ProductId == productId);
                if (product != null)
                {
                    if (message.ToLower().Contains("năm sản xuất"))
                    {
                        return await GetGptResponse(message, product);
                    }
                    else if (message.ToLower().Contains("hãng"))
                    {
                        return await GetGptResponse(message, product);
                    }
                    return await GetGptResponse(message, product);
                }
                return await GetGptResponse(message, null);
            }
            return Json(new { reply = "Xin lỗi nha, bạn chưa nói rõ muốn hỏi về sản phẩm nào! Cứ chỉ mình, mình giúp ngay!" });
        }

        private async Task<JsonResult> GetGptResponse(string message, Product product = null)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

                    var context = product != null
                        ? $"Sản phẩm: {product.ProductName}, Giá: ${product.Price}, Mô tả: {product.Description}, Số lượng: {product.QuantityInStock}. " +
                          $"Không có thông tin chính xác về năm sản xuất hoặc hãng trong kho, nhưng cứ trả lời sáng tạo, hợp lý nha. Câu hỏi của khách: {message}"
                        : message;

                    var requestBody = new
                    {
                        model = "gpt-4",
                        messages = new[]
                        {
                            new {
                                role = "system",
                                content = "Bạn là chủ cửa hàng Electro, thân thiện, nhiệt tình và luôn sẵn sàng giúp khách hàng. Trả lời tự nhiên như người bán hàng thực thụ, dùng ngôn ngữ gần gũi, khuyến khích khách hỏi thêm hoặc mua sản phẩm. Nếu không có thông tin chính xác, hãy trả lời sáng tạo nhưng hợp lý, và giữ thái độ tích cực." +
                                "Thông tin liên hệ cửa hàng mail : Anhpbqbbh00610@gmail.com - Phone : 0358378692"
                                +"Địa chỉ : So 44 , Bac Tu Niem , Ha Noi"
                                +"Giao hàng rồi mới thanh toán , tính toán ngày giao dựa trên khoảng cách địa lý"
                                +"chỉ trả lời tập trung vào câu hỏi"
                                +"các sản phẩm ko rõ năm sản xuất thì tự chế vào"
                                +"Hiện tại chỉ có phương thức thanh toán khi nhận hàng"
                                +"có thể trả góp bằng thẻ tín dụng , trả trước 30% với các đơn hàng từ 100 đô trở lên"

                            },
                            new { role = "user", content = context }
                        },
                        max_tokens = 500
                    };

                    var jsonContent = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");
                    var response = await client.PostAsync(_apiUrl, jsonContent);

                    if (response.IsSuccessStatusCode)
                    {
                        var responseData = await response.Content.ReadAsStringAsync();
                        var result = JsonConvert.DeserializeObject<dynamic>(responseData);
                        string chatbotReply = result.choices[0].message.content;
                        _lastResponse = chatbotReply;
                        return Json(new { reply = chatbotReply });
                    }

                    return Json(new { reply = "Ôi, hệ thống hơi trục trặc chút rồi! Để mình thử lại cho bạn nha!" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { reply = $"Ôi, sorry bạn nha, hệ thống hơi lag chút xíu rồi! Để mình thử lại cho bạn sau nhé, hoặc bạn cứ hỏi mình thêm gì cũng được!" });
            }
        }
    }
}