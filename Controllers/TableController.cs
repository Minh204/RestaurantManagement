using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using RestaurantManagement.Models;
using System.Data;

namespace RestaurantManagement.Controllers
{
    [Authorize(Roles = "Admin, Staff")]
    public class TableController : Controller
    {
        private readonly IConfiguration _config;

        public TableController(IConfiguration config)
        {
            _config = config;
        }

        public IActionResult Index()
        {
            var tables = new List<TableViewModel>();
            using (var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
            {
                conn.Open();
                var cmd = new SqlCommand("sp_GetAllTables", conn)
                {
                    CommandType = CommandType.StoredProcedure
                };

                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    tables.Add(new TableViewModel
                    {
                        Id = (int)reader["Id"],
                        TableName = reader["TableName"].ToString(),
                        FloorName = reader["FloorName"].ToString(),
                        TableStatus = reader["TableStatus"].ToString()
                    });
                }
            }
            return View(tables);
        }

        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.Floors = GetFloors();
            return View();
        }

        [HttpPost]
        public IActionResult Create(TableInputModel model)
        {
            using (var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
            {
                conn.Open();
                var cmd = new SqlCommand("sp_AddTable", conn)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@TableName", model.TableName);
                cmd.Parameters.AddWithValue("@FloorId", model.FloorId);
                cmd.Parameters.AddWithValue("@TableStatus", model.TableStatus);
                cmd.ExecuteNonQuery();
            }
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            TableInputModel model = new TableInputModel();
            using (var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
            {
                conn.Open();
                var cmd = new SqlCommand("SELECT * FROM ResTable WHERE Id = @Id", conn);
                cmd.Parameters.AddWithValue("@Id", id);
                var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    model.Id = id;
                    model.TableName = reader["TableName"].ToString();
                    model.FloorId = (int)reader["FloorId"];
                    model.TableStatus = reader["TableStatus"].ToString();
                }
            }
            ViewBag.Floors = GetFloors();
            return View(model);
        }

        [HttpPost]
        public IActionResult Edit(TableInputModel model)
        {
            using (var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
            {
                conn.Open();
                var cmd = new SqlCommand("sp_UpdateTable", conn)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@Id", model.Id);
                cmd.Parameters.AddWithValue("@TableName", model.TableName);
                cmd.Parameters.AddWithValue("@FloorId", model.FloorId);
                cmd.Parameters.AddWithValue("@TableStatus", model.TableStatus);
                cmd.ExecuteNonQuery();
            }
            return RedirectToAction("Index");
        }

        public IActionResult Delete(int id)
        {
            using (var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
            {
                conn.Open();
                var cmd = new SqlCommand("sp_DeleteTable", conn)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@Id", id);
                cmd.ExecuteNonQuery();
            }
            return RedirectToAction("Index");
        }

        private List<SelectListItem> GetFloors()
        {
            var list = new List<SelectListItem>();
            using (var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
            {
                conn.Open();
                var cmd = new SqlCommand("SELECT Id, FloorName FROM Floors", conn);
                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(new SelectListItem
                    {
                        Value = reader["Id"].ToString(),
                        Text = reader["FloorName"].ToString()
                    });
                }
            }
            return list;
        }
    }
}
