using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using RestaurantManagement.Models;
using System.Data;

namespace RestaurantManagement.Controllers
{
    public class FoodController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;

        public FoodController(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("DefaultConnection");
        }
        [Authorize(Roles = "Admin")]
        public IActionResult Index(int page = 1, int pageSize = 5)
        {
            var foods = new List<FoodViewModel>();
            int totalItems = 0;

            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                var countCmd = new SqlCommand("SELECT COUNT(*) FROM Food", conn);
                totalItems = (int)countCmd.ExecuteScalar();

                var offset = (page - 1) * pageSize;
                var cmd = new SqlCommand(@"
                SELECT f.Id, f.FoodName, c.CategoryName, f.Describe, f.ImageUrl, f.Price, f.IsActive
                FROM Food f
                JOIN Category c ON f.CategoryId = c.Id
                ORDER BY f.Id
                OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY", conn);

                cmd.Parameters.AddWithValue("@offset", offset);
                cmd.Parameters.AddWithValue("@pageSize", pageSize);

                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    foods.Add(new FoodViewModel
                    {
                        Id = (int)reader["Id"],
                        FoodName = reader["FoodName"].ToString(),
                        CategoryName = reader["CategoryName"].ToString(),
                        Describe = reader["Describe"].ToString(),
                        ImageUrl = reader["ImageUrl"].ToString(),
                        Price = (decimal)reader["Price"],
                        IsActive = (bool)reader["IsActive"]
                    });
                }
            }

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            return View(foods);
        }
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.Categories = GetCategories();
            return View();
        }
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public IActionResult Create(FoodViewModel model, IFormFile imageFile)
        {
            if (imageFile != null)
            {
                var fileName = Path.GetFileName(imageFile.FileName);
                var filePath = Path.Combine("wwwroot/images/food", fileName);
                using var stream = new FileStream(filePath, FileMode.Create);
                imageFile.CopyTo(stream);
                model.ImageUrl = "/images/food/" + fileName;
            }

            using var conn = new SqlConnection(_connectionString);
            conn.Open();
            var cmd = new SqlCommand(@"INSERT INTO Food (FoodName, CategoryId, Describe, ImageUrl, Price, IsActive)
                                   VALUES (@FoodName, @CategoryId, @Description, @Image, @Price, 1)", conn);
            cmd.Parameters.AddWithValue("@FoodName", model.FoodName);
            cmd.Parameters.AddWithValue("@CategoryId", model.CategoryId);
            cmd.Parameters.AddWithValue("@Description", model.Describe ?? "");
            cmd.Parameters.AddWithValue("@Image", model.ImageUrl ?? "");
            cmd.Parameters.AddWithValue("@Price", model.Price);
            cmd.ExecuteNonQuery();

            return RedirectToAction("Index");
        }
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult Edit(int id)
        {
            var model = new FoodViewModel();
            using var conn = new SqlConnection(_connectionString);
            conn.Open();
            var cmd = new SqlCommand("SELECT * FROM Food WHERE Id = @Id", conn);
            cmd.Parameters.AddWithValue("@Id", id);
            var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                model.Id = id;
                model.FoodName = reader["FoodName"].ToString();
                model.CategoryId = (int)reader["CategoryId"];
                model.Describe = reader["Describe"].ToString();
                model.ImageUrl= reader["ImageUrl"].ToString();
                model.Price = (decimal)reader["Price"];
                model.IsActive = (bool)reader["IsActive"];
            }

            ViewBag.Categories = GetCategories();
            return View(model);
        }
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public IActionResult Edit(FoodViewModel model, IFormFile? imageFile)
        {
            if (imageFile != null)
            {
                var fileName = Path.GetFileName(imageFile.FileName);
                var filePath = Path.Combine("wwwroot/images/food", fileName);
                using var stream = new FileStream(filePath, FileMode.Create);
                imageFile.CopyTo(stream);
                model.ImageUrl = "/images/food/" + fileName;
            }

            using var conn = new SqlConnection(_connectionString);
            conn.Open();
            var cmd = new SqlCommand(@"UPDATE Food 
            SET FoodName = @FoodName, CategoryId = @CategoryId, Describe = @Description, 
                ImageUrl = @Image, Price = @Price, IsActive = @Status 
            WHERE Id = @Id", conn);
            cmd.Parameters.AddWithValue("@Id", model.Id);
            cmd.Parameters.AddWithValue("@FoodName", model.FoodName);
            cmd.Parameters.AddWithValue("@CategoryId", model.CategoryId);
            cmd.Parameters.AddWithValue("@Description", model.Describe ?? "");
            cmd.Parameters.AddWithValue("@Image", model.ImageUrl ?? "");
            cmd.Parameters.AddWithValue("@Price", model.Price);
            cmd.Parameters.AddWithValue("@Status", model.IsActive);
            cmd.ExecuteNonQuery();

            return RedirectToAction("Index");
        }
        [Authorize(Roles = "Admin")]
        public IActionResult Delete(int id)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();
            var cmd = new SqlCommand("DELETE FROM Food WHERE Id = @Id", conn);
            cmd.Parameters.AddWithValue("@Id", id);
            cmd.ExecuteNonQuery();
            return RedirectToAction("Index");
        }
        public IActionResult FoodDetail(int id)
        {
            var model = new FoodDetailViewModel();

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                string sql = @"SELECT f.Id, f.FoodName, f.ImageUrl, f.Price,
                              fd.Detail, fd.Ingredient, fd.Ration, fd.CookTime
                       FROM Food f
                       INNER JOIN FoodDetail fd ON f.Id = fd.FoodId
                       WHERE f.Id = @Id";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            model.Id = (int)reader["Id"];
                            model.FoodName = reader["FoodName"].ToString();
                            model.ImageUrl = reader["ImageUrl"].ToString();
                            model.Price = (decimal)reader["Price"];
                            model.Detail = reader["Detail"].ToString();
                            model.Ingredient = reader["Ingredient"].ToString();
                            model.Ration = reader["Ration"].ToString();
                            model.CookTime = reader["CookTime"].ToString();
                        }
                    }
                }
            }

            return View(model);
        }
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult Export()
        {
            var foods = new List<FoodViewModel>();

            using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                conn.Open();
                var cmd = new SqlCommand(@"
                SELECT f.Id, f.FoodName, c.CategoryName, f.Describe, f.ImageUrl, f.Price, f.IsActive
                FROM Food f
                JOIN Category c ON f.CategoryId = c.Id", conn);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        foods.Add(new FoodViewModel
                        {
                            FoodName = reader["FoodName"].ToString(),
                            CategoryName = reader["CategoryName"].ToString(),
                            Describe = reader["Describe"].ToString(),
                            ImageUrl = reader["ImageUrl"].ToString(),
                            Price = (decimal)reader["Price"],
                            IsActive = (bool)reader["IsActive"]
                        });
                    }
                }
            }

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Danh sách món ăn");

            // Header
            worksheet.Cell(1, 1).Value = "Tên món";
            worksheet.Cell(1, 2).Value = "Danh mục";
            worksheet.Cell(1, 3).Value = "Mô tả";
            worksheet.Cell(1, 4).Value = "Ảnh (URL)";
            worksheet.Cell(1, 5).Value = "Giá";
            worksheet.Cell(1, 6).Value = "Trạng thái";

            // Dữ liệu
            for (int i = 0; i < foods.Count; i++)
            {
                var row = i + 2;
                worksheet.Cell(row, 1).Value = foods[i].FoodName;
                worksheet.Cell(row, 2).Value = foods[i].CategoryName;
                worksheet.Cell(row, 3).Value = foods[i].Describe;
                worksheet.Cell(row, 4).Value = foods[i].ImageUrl;
                worksheet.Cell(row, 5).Value = foods[i].Price;
                worksheet.Cell(row, 6).Value = foods[i].IsActive ? "Đang bán" : "Ngừng bán";
            }

            // Auto fit
            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"DanhSachMonAn_{DateTime.Now:yyyyMMddHHmmss}.xlsx");
        }

        private List<Category> GetCategories()
        {
            var list = new List<Category>();
            using var conn = new SqlConnection(_connectionString);
            conn.Open();
            var cmd = new SqlCommand("SELECT * FROM Category", conn);
            var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new Category
                {
                    Id = (int)reader["Id"],
                    CategoryName = reader["CategoryName"].ToString()
                });
            }
            return list;
        }
    }
}
