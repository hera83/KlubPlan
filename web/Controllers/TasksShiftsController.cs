using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace web.Controllers
{
    [Authorize]
    public class TasksShiftsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
