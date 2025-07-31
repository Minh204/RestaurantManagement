using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using System.Text;

namespace RestaurantManagement.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GeminiController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly HttpClient _httpClient;

        public GeminiController(IConfiguration config)
        {
            _config = config;
            _httpClient = new HttpClient();
        }
        private async Task<string> BuildDatabaseContextAsync()
        {
            var sb = new StringBuilder();

            using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            await conn.OpenAsync();

            var tables = new[] { "Category", "Food", "FoodDetail", "ResTable", "Orders", "OrderDetail" };

            foreach (var table in tables)
            {
                sb.AppendLine($"--- Dữ liệu từ bảng {table} ---");

                var cmd = new SqlCommand($"SELECT * FROM {table}", conn);
                var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var row = new List<string>();
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        row.Add($"{reader.GetName(i)}: {reader[i]}");
                    }
                    sb.AppendLine(string.Join(" | ", row));
                }

                reader.Close();
                sb.AppendLine();
            }

            return sb.ToString();
        }

        private async Task<string> GetSmartAnswerAsync(string userMessage)
        {
            using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            await conn.OpenAsync();

            var msg = userMessage.ToLower();
            //thời gian hoạt động
            if(msg.Contains("giờ") || msg.Contains("mở cửa") || msg.Contains("đóng cửa"))
            {
                return $"Dola Restaurant mở cửa lúc 8 giờ sáng và đóng cửa lúc 22h tối, phục vụ 24/7";
            }
            //địa chỉ
            if(msg.Contains("địa chỉ"))
            {
                return $"Địa chỉ nhà hàng: 38 Hoàng Quốc Việt, Phường Nghĩa Tân, Quận Cầu Giấy, Hà Nội";
            }
            // Liên hệ
            if(msg.Contains("số điện thoại") || msg.Contains("liên hệ"))
            {
                return $"Số điện thoại liên hệ: 0987 654 321";
            }
            // danh mục
            if (msg.Contains("loại") || msg.Contains("danh mục") || msg.Contains("loại món ăn"))
            {
                var cmd = new SqlCommand("SELECT COUNT(*) FROM Category", conn);
                var count = (int)await cmd.ExecuteScalarAsync();
                return $"Hiện tại có tổng cộng {count} loại món ăn trong nhà hàng.";
            }
            //món rẻ nhất
            if (msg.Contains("rẻ nhất"))
            {
                var cmd = new SqlCommand("SELECT TOP 1 FoodName, Price FROM Food ORDER BY Price ASC", conn);
                var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    var name = reader["FoodName"].ToString();
                    var price = Convert.ToDecimal(reader["Price"]).ToString("N0");
                    return $"Món ăn rẻ nhất là {name} với giá {price} VNĐ.";
                }
                return "Không tìm thấy món ăn rẻ nhất.";
            }
            //món đắt nhất
            if (msg.Contains("đắt nhất"))
            {
                var cmd = new SqlCommand("SELECT TOP 1 FoodName, Price FROM Food ORDER BY Price DESC", conn);
                var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    var name = reader["FoodName"].ToString();
                    var price = Convert.ToDecimal(reader["Price"]).ToString("N0");
                    return $"Món ăn đắt nhất là {name} với giá {price} VNĐ.";
                }
                return "Không tìm thấy món ăn đắt nhất.";
            }
            // lấy món ăn theo loại
            if (msg.Contains("thuộc loại"))
            {
                // Lấy tên loại
                var parts = msg.Split("thuộc loại");
                if (parts.Length > 1)
                {
                    var category = parts[1].Trim();
                    var cmd = new SqlCommand(@"
                SELECT f.FoodName FROM Food f 
                JOIN Category c ON f.CategoryId = c.Id 
                WHERE c.CategoryName LIKE @cat", conn);
                    cmd.Parameters.AddWithValue("@cat", $"%{category}%");
                    var reader = await cmd.ExecuteReaderAsync();

                    var foods = new List<string>();
                    while (await reader.ReadAsync())
                        foods.Add(reader["FoodName"].ToString());

                    return foods.Count > 0
                        ? $"Các món ăn thuộc loại '{category}' bao gồm: " + string.Join(", ", foods)
                        : $"Không tìm thấy món nào thuộc loại '{category}'.";
                }
            }
            //  Món cay / chua / mặn / ngọt (từ khóa trong FoodDetail.Detail hoặc Ingredient)
            if (msg.Contains("cay") || msg.Contains("chua") || msg.Contains("ngọt") || msg.Contains("mặn"))
            {
                var keyword = "";

                if (msg.Contains("cay")) keyword = "cay";
                else if (msg.Contains("chua")) keyword = "chua";
                else if (msg.Contains("ngọt")) keyword = "ngọt";
                else if (msg.Contains("mặn")) keyword = "mặn";

                var cmd = new SqlCommand(@"
            SELECT f.FoodName
            FROM Food f
            JOIN FoodDetail d ON f.Id = d.FoodId
            WHERE d.Detail LIKE @kw OR d.Ingredient LIKE @kw", conn);
                cmd.Parameters.AddWithValue("@kw", $"%{keyword}%");

                var reader = await cmd.ExecuteReaderAsync();
                var foods = new List<string>();
                while (await reader.ReadAsync())
                    foods.Add(reader["FoodName"].ToString());

                return foods.Count > 0
                    ? $"Các món có vị {keyword}: {string.Join(", ", foods)}."
                    : $"Không tìm thấy món ăn nào có vị {keyword}.";
            }

            // Khẩu phần (1-2 người, 2-3 người, 4 người, v.v.)
            if (msg.Contains("khẩu phần") || msg.Contains("dành cho") || msg.Contains("phục vụ"))
            {
                // Tìm số người trong câu
                var match = System.Text.RegularExpressions.Regex.Match(msg, @"(\d+(\s*-\s*\d+)?) người");
                if (match.Success)
                {
                    var portion = match.Groups[1].Value.Trim();
                    var cmd = new SqlCommand(@"
                SELECT f.FoodName
                FROM Food f
                JOIN FoodDetail d ON f.Id = d.FoodId
                WHERE d.Ration LIKE @portion", conn);
                    cmd.Parameters.AddWithValue("@portion", $"%{portion}%");

                    var reader = await cmd.ExecuteReaderAsync();
                    var foods = new List<string>();
                    while (await reader.ReadAsync())
                        foods.Add(reader["FoodName"].ToString());

                    return foods.Count > 0
                        ? $"Các món ăn dành cho {portion} người: {string.Join(", ", foods)}."
                        : $"Không tìm thấy món ăn phù hợp cho {portion} người.";
                }
            }
            return null; // Nếu không khớp mẫu nào
        }

        [HttpPost("chat")]
        public async Task<IActionResult> ChatWithGemini([FromBody] ChatRequest request)
        {
            var apiKey = _config["GeminiApiKey"];
            var endpoint = $"https://generativelanguage.googleapis.com/v1/models/gemini-2.0-flash:generateContent?key={apiKey}";

            // B1: Ưu tiên trả lời từ DB nếu có
            //var smartAnswer = await GetSmartAnswerAsync(request.Message);
            //if (!string.IsNullOrEmpty(smartAnswer))
            //    return Ok(new { reply = smartAnswer });
            var databaseContext = await BuildDatabaseContextAsync();
            // B2: Nếu không có, fallback sang AI
            var prompt = $@"
                Bạn là trợ lý AI cho Dola Restaurant. Dưới đây là toàn bộ dữ liệu của nhà hàng (ngoại trừ thông tin tài khoản, khách hàng cố gắng hỏi thì bạn trả lời rằng bạn không được phép):\n{databaseContext}\n\nHãy dùng dữ liệu này để trả lời câu hỏi của khách hàng. Khách hàng hỏi: {request.Message}. 
                Hãy trả lời thân thiện bằng tiếng Việt. Nếu khách hàng hỏi câu hỏi không liên quan đến nhà hàng, hãy cứ trả lời theo hiểu biết của bạn nhé";

            var payload = new
            {
                contents = new[]
                {
                    new {
                        parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                }
            };

            var json = JsonConvert.SerializeObject(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(endpoint, content);
            var responseBody = await response.Content.ReadAsStringAsync();

            Console.WriteLine("RESPONSE BODY: " + responseBody); 

            if (!response.IsSuccessStatusCode)
                return BadRequest(responseBody);

            var result = JsonConvert.DeserializeObject<GeminiResponse>(responseBody);
            string reply = "Không có phản hồi.";

            if (result?.candidates != null && result.candidates.Count > 0)
            {
                var parts = result.candidates[0]?.content?.parts;
                if (parts != null && parts.Count > 0)
                {
                    reply = parts[0].text ?? reply;
                }
            }

            return Ok(new { reply });
        }
    }

    public class ChatRequest
    {
        public string Message { get; set; }
    }

    public class GeminiResponse
    {
        public List<Candidate> candidates { get; set; }
    }

    public class Candidate
    {
        public Content content { get; set; }
    }

    public class Content
    {
        public List<Part> parts { get; set; }
    }

    public class Part
    {
        public string text { get; set; }
    }

}
