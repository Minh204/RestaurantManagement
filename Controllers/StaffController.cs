using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace RestaurantManagement.Controllers
{
    public class StaffController : Controller
    {
        [Authorize(Roles = "Staff")]
        public IActionResult Index()
        {
            var fullName = User.FindFirst("FullName").Value;
            ViewBag.FullName = fullName;
            return View();
        }
    }
}
