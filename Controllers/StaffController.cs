using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace RestaurantManagement.Controllers
{
    public class StaffController : Controller
    {
        [Authorize(Roles = "Staff")]
        public IActionResult Index()
        {
            return View();
        }
    }
}
