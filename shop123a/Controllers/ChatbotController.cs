using System.Text;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net.Http;
using System.Threading.Tasks;
using shop123a.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;

namespace shop123a.Controllers
{
    public class ChatbotController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly Shop123Context _db;

        public ChatbotController(Shop123Context context, IHttpClientFactory httpClientFactory)
        {
            _db = context;
            _httpClientFactory = httpClientFactory;
        }

        [HttpPost]
        public async Task<IActionResult> SendMessage([FromBody] ChatRequest request)
        {
            try
            {
                var chatHistory = HttpContext.Session.GetString(ChatHistoryKey);
                var messages = string.IsNullOrEmpty(chatHistory)
                    ? new List<ChatMessage>()
                    : JsonConvert.DeserializeObject<List<ChatMessage>>(chatHistory);

                var hasFoundProducts = HttpContext.Session.GetString("HasFoundProducts") == "true";
                var searchTerms = request.Message.ToLower().Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                string replyText;
                List<object> products = new List<object>();

                if (messages.Count == 0)
                {
                    replyText = "Chào bạn! Rất vui được giúp bạn hôm nay nha. Bạn đang tìm gì vậy, nói mình nghe nào? 😊";
                }
                else
                {
                    if (hasFoundProducts && !IsSearchIntent(request.Message))
                    {
                        replyText = await GetOpenAIResponse(request.Message, messages);
                    }
                    else
                    {
                        var categoryMatch = await FindBestCategoryMatch(searchTerms);
                        if (categoryMatch.category != null)
                        {
                            var productsByCategory = await GetProductsByCategory(categoryMatch.category.CategoryId, searchTerms);
                            if (productsByCategory.Any())
                            {
                                replyText = $"Chào bạn! Mình tìm thấy mấy sản phẩm siêu xịn trong danh mục '{categoryMatch.category.CategoryName}' nè:";
                                products = productsByCategory.Select(p => (object)new
                                {
                                    name = p.product.ProductName,
                                    matchScore = (int)(p.score * 100),
                                    price = p.product.Price.ToString("C"),
                                    description = p.product.Description ?? "Chưa có mô tả nha",
                                    quantity = p.product.QuantityInStock,
                                    imageUrl = p.product.ImageUrl ?? "/img/default-product.jpg",
                                    productUrl = $"/HangHoa/Detail/{p.product.ProductId}"
                                }).ToList();
                                HttpContext.Session.SetString("HasFoundProducts", "true");
                            }
                            else
                            {
                                replyText = $"Ôi, mình tìm trong danh mục '{categoryMatch.category.CategoryName}' mà chưa thấy sản phẩm nào phù hợp. Bạn thử nói rõ hơn nhé, mình sẽ cố hết sức giúp bạn! 😊";
                            }
                        }
                        else
                        {
                            var productsSmart = await SearchProductsSmart(searchTerms);
                            if (productsSmart.Any())
                            {
                                replyText = "Yeah! Mình tìm được vài món cực hợp với bạn nè:";
                                products = productsSmart.Select(p => (object)new
                                {
                                    name = p.product.ProductName,
                                    matchScore = (int)(p.score * 100),
                                    price = p.product.Price.ToString("C"),
                                    description = p.product.Description ?? "Chưa có mô tả nha",
                                    quantity = p.product.QuantityInStock,
                                    imageUrl = p.product.ImageUrl ?? "/img/default-product.jpg",
                                    productUrl = $"/HangHoa/Detail/{p.product.ProductId}"
                                }).ToList();
                                HttpContext.Session.SetString("HasFoundProducts", "true");
                            }
                            else
                            {
                                replyText = await GetOpenAIResponse(request.Message, messages);
                            }
                        }
                    }
                }

                // Cập nhật lịch sử chat, chỉ lưu trữ replyText
                messages.Add(new ChatMessage { Role = "user", Content = request.Message });
                messages.Add(new ChatMessage { Role = "assistant", Content = replyText });

                if (messages.Count > 10)
                    messages = messages.GetRange(messages.Count - 10, 10);

                HttpContext.Session.SetString(ChatHistoryKey, JsonConvert.SerializeObject(messages));

                // Trả về phản hồi chỉ với text và products (nếu có)
                return Json(new { success = true, message = new { text = replyText, products } });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = new { text = "Ôi không! Có lỗi gì đó rồi, để mình sửa ngay nhé: " + ex.Message, products = new List<object>() } });
            }
        }

        private async Task<(Category category, float score)> FindBestCategoryMatch(string[] searchTerms)
        {
            var categories = await _db.Categories.ToListAsync();
            Category bestMatch = null;
            float highestScore = 0f;

            foreach (var category in categories)
            {
                float score = CalculateMatchScore(category.CategoryName.ToLower(), searchTerms);
                if (score > highestScore && score >= 0.6f)
                {
                    highestScore = score;
                    bestMatch = category;
                }
            }

            return (bestMatch, highestScore);
        }

        private async Task<List<(Product product, float score)>> GetProductsByCategory(int categoryId, string[] searchTerms)
        {
            var products = await _db.Products
                .Where(p => p.CategoryId == categoryId)
                .ToListAsync();

            return products
                .Select(p => (product: p, score: CalculateMatchScore(
                    $"{p.ProductName.ToLower()} {(p.Description ?? "").ToLower()}",
                    searchTerms)))
                .Where(p => p.score > 0.5f)
                .OrderByDescending(p => p.score)
                .Take(5)
                .ToList();
        }

        private async Task<List<(Product product, float score)>> SearchProductsSmart(string[] searchTerms)
        {
            var products = await _db.Products.ToListAsync();

            return products
                .Select(p => (product: p, score: CalculateMatchScore(
                    $"{p.ProductName.ToLower()} {(p.Description ?? "").ToLower()}",
                    searchTerms)))
                .Where(p => p.score > 0.5f)
                .OrderByDescending(p => p.score)
                .Take(3)
                .ToList();
        }

        private float CalculateMatchScore(string text, string[] searchTerms)
        {
            if (string.IsNullOrEmpty(text) || searchTerms.Length == 0) return 0f;

            float totalScore = 0f;
            int matches = 0;

            foreach (var term in searchTerms)
            {
                if (text.Contains(term))
                {
                    matches++;
                    totalScore += 1f;
                }
                else
                {
                    int minDistance = term.Length;
                    foreach (var word in text.Split(' '))
                    {
                        int distance = LevenshteinDistance(term, word);
                        if (distance < minDistance) minDistance = distance;
                    }
                    if (minDistance <= 2)
                    {
                        float partialScore = 1f - (minDistance / (float)term.Length);
                        totalScore += partialScore;
                        matches++;
                    }
                }
            }

            return matches > 0 ? totalScore / searchTerms.Length : 0f;
        }

        private int LevenshteinDistance(string s, string t)
        {
            if (string.IsNullOrEmpty(s)) return t?.Length ?? 0;
            if (string.IsNullOrEmpty(t)) return s.Length;

            int[,] d = new int[s.Length + 1, t.Length + 1];
            for (int i = 0; i <= s.Length; i++) d[i, 0] = i;
            for (int j = 0; j <= t.Length; j++) d[0, j] = j;

            for (int i = 1; i <= s.Length; i++)
            {
                for (int j = 1; j <= t.Length; j++)
                {
                    int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }
            return d[s.Length, t.Length];
        }

        private async Task<string> GetOpenAIResponse(string userMessage, List<ChatMessage> chatHistory)
        {
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

            var messageList = new List<object>
            {
                new {
                    role = "system",
                    content = "Bạn là một chủ cửa hàng thân thiện, nhiệt tình và vui tính. Hãy trả lời khách hàng bằng giọng điệu gần gũi, tự nhiên, đúng ngữ pháp tiếng Việt, thêm emoji nếu phù hợp, và luôn khuyến khích khách hỏi thêm hoặc mua hàng. Khi trả lời về sản phẩm, hãy sử dụng thông tin sản phẩm (tên, giá, mô tả) để trả lời chính xác, không cần đề cập đến số lượng sản phẩm. Ví dụ: 'Mình có laptop Acer Aspire Lite 14 với giá $1,299.99 nè. Máy này phục vụ tốt cho công việc học tập và làm việc hàng ngày luôn đó bạn! 😊'"
                    + "Thông tin liên hệ cửa hàng mail : Anhpbqbbh00610@gmail.com - Phone : 0358378692"
                                +"Địa chỉ : So 44 , Bac Tu Niem , Ha Noi"
                                +"Giao hàng rồi mới thanh toán , tính toán ngày giao dựa trên khoảng cách địa lý"
                                +"chỉ trả lời tập trung vào câu hỏi"
                                +"các sản phẩm ko rõ năm sản xuất thì tự chế vào"
                                +"Hiện tại chỉ có phương thức thanh toán khi nhận hàng"
                                +"có thể trả góp bằng thẻ tín dụng , trả trước 30% với các đơn hàng từ 100 đô trở lên"
                }
            };

            if (userMessage.ToLower().Contains("rẻ nhất"))
            {
                var cheapestProduct = await _db.Products
                    .OrderBy(p => p.Price)
                    .FirstOrDefaultAsync();

                if (cheapestProduct != null)
                {
                    messageList.Add(new
                    {
                        role = "system",
                        content = $"Sản phẩm rẻ nhất hi ện tại là: {cheapestProduct.ProductName}, giá {cheapestProduct.Price.ToString("C")}, mô tả: {cheapestProduct.Description ?? "Chưa có mô tả"}."
                    });
                }
            }

            messageList.AddRange(chatHistory.Select(m => new { role = m.Role, content = m.Content }));
            messageList.Add(new { role = "user", content = userMessage });

            var message = new
            {
                model = "gpt-4",
                messages = messageList.ToArray(),
                temperature = 0.9,
                max_tokens = 500
            };

            var jsonContent = JsonConvert.SerializeObject(message);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await client.PostAsync(OpenAIEndpoint, content);
            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();
            var responseObject = JsonConvert.DeserializeObject<OpenAIResponse>(responseString);

            return responseObject.choices[0].message.content;
        }

        private bool IsSearchIntent(string message)
        {
            var lowerMessage = message.ToLower();
            string[] searchKeywords = { "tìm", "search", "có", "hàng", "sản phẩm", "mua", "rẻ nhất", "giá thấp" };
            return searchKeywords.Any(k => lowerMessage.Contains(k)) || lowerMessage.Split(' ').Length <= 3;
        }
    }

    public class ChatMessage
    {
        public string Role { get; set; }
        public string Content { get; set; }
    }

    public class ChatRequest
    {
        public string Message { get; set; }
    }

    public class OpenAIResponse
    {
        public Choice[] choices { get; set; }
    }

    public class Choice
    {
        public Message message { get; set; }
    }

    public class Message
    {
        public string role { get; set; }
        public string content { get; set; }
    }
}