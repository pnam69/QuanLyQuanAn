using Microsoft.AspNetCore.Mvc;

namespace QuanLyQuanAn.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            ViewBag.HoTen = HttpContext.Session.GetString("HoTen");
            ViewBag.VaiTro = HttpContext.Session.GetString("VaiTro");

            return View();
        }

        [HttpGet]
        public IActionResult Privacy()
        {
            return RedirectToAction("Privacy", "Home");
        }
    }
}