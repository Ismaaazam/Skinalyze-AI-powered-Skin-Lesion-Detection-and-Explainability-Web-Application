using Microsoft.AspNetCore.Mvc;

namespace Skinalyze.Controllers
{
    public class ChatbotController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
