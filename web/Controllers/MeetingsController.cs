using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace web.Controllers
{
    [Authorize]
    public class MeetingsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
