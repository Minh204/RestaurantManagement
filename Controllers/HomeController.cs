using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using RestaurantManagement.Models;
using System.Data;
using System.Diagnostics;

namespace RestaurantManagement.Controllers
{
    public class HomeController : Controller
    {
        private readonly string _connStr;
        public HomeController(IConfiguration config)
        {
            _connStr = config.GetConnectionString("DefaultConnection");
        }
        public IActionResult Index()
        {
            var featuredFoods = new List<FoodViewModel>();
            using (SqlConnection conn = new SqlConnection(_connStr))
            {
                SqlCommand cmd = new SqlCommand("sp_GetFeaturedFoods", conn)
                {
                    CommandType = System.Data.CommandType.StoredProcedure
                };
                conn.Open();
                using SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    featuredFoods.Add(new FoodViewModel
                    {
                        Id = reader.GetInt32(0),
                        FoodName = reader.GetString(1),
                        Describe = reader.GetString(2),
                        Price = reader.GetDecimal(3),
                        ImageUrl = reader.GetString(4)
                    });
                }
            }
            return View(featuredFoods);
        }
        public IActionResult Menu(int? categoryId)
        {
            var categories = new List<Category>();
            var foods = new List<FoodViewModel>();

            using (SqlConnection conn = new SqlConnection(_connStr))
            {
                conn.Open();

                // Lấy danh mục
                using (SqlCommand cmd = new SqlCommand("sp_GetAllCategories", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    var reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        categories.Add(new Category
                        {
                            Id = (int)reader["Id"],
                            CategoryName = reader["CategoryName"].ToString(),
                            Describe = reader["Describe"]?.ToString()
                        });
                    }
                    reader.Close();
                }

                // Lấy danh sách món ăn
                if (categoryId.HasValue)
                {
                    using (SqlCommand cmd = new SqlCommand("sp_GetFoodsByCategoryId", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@CategoryId", categoryId.Value);
                        var reader = cmd.ExecuteReader();
                        while (reader.Read())
                        {
                            foods.Add(new FoodViewModel
                            {
                                Id = (int)reader["Id"],
                                FoodName = reader["FoodName"].ToString(),
                                Describe = reader["Describe"].ToString(),
                                ImageUrl = reader["ImageUrl"]?.ToString(),
                                Price = (decimal)reader["Price"],
                                CategoryId = (int)reader["CategoryId"]

                            });

                        }
                        reader.Close();
                    }
                }
                else
                {
                    using (SqlCommand cmd = new SqlCommand("sp_GetAllFoods", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        var reader = cmd.ExecuteReader();
                        while (reader.Read())
                        {
                            foods.Add(new FoodViewModel
                            {
                                Id = (int)reader["Id"],
                                FoodName = reader["FoodName"].ToString(),
                                Describe = reader["Describe"].ToString(),
                                ImageUrl = reader["ImageUrl"]?.ToString(),
                                Price = (decimal)reader["Price"],
                                CategoryId = (int)reader["CategoryId"]
                            });
                        }
                        reader.Close();
                    }
                }

                conn.Close();
            }

            var model = new MenuViewModel
            {
                Categories = categories,
                Foods = foods,
                SelectedCategoryId = categoryId
            };

            return View(model);
        }
        [HttpGet]
        public IActionResult Search(string keyword, string sortOrder = "asc")
        {
            var list = new List<FoodSearchViewModel>();
            using (SqlConnection conn = new SqlConnection(_connStr))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand("SearchFoodByKeyword", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@keyword", keyword ?? "");

                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(new FoodSearchViewModel
                    {
                        Id = (int)reader["Id"],
                        FoodName = reader["FoodName"].ToString(),
                        Description = reader["Describe"].ToString(),
                        Image = reader["ImageUrl"].ToString(),
                        Price = Convert.ToDecimal(reader["Price"]),
                        CategoryName = reader["CategoryName"].ToString()
                    });
                }
            }

            // Sắp xếp
            list = sortOrder == "desc"
                ? list.OrderByDescending(f => f.Price).ToList()
                : list.OrderBy(f => f.Price).ToList();

            ViewBag.Keyword = keyword;
            ViewBag.SortOrder = sortOrder;
            return View(list);
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
}
