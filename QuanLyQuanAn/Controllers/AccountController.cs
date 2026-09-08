using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyQuanAn.Data;

namespace QuanLyQuanAn.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;

        public AccountController(AppDbContext context)
        {
            _context = context;
        }
        public IActionResult AccessDenied()
        {
            return View();
        }
        // GET: Account/Login
        [HttpGet]
        public IActionResult Login()
        {
            if (HttpContext.Session.GetInt32("MaNV") != null)
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        // POST: Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(
            string TaiKhoan,
            string MatKhau)
        {
            if (string.IsNullOrWhiteSpace(TaiKhoan))
            {
                ModelState.AddModelError(
                    "TaiKhoan",
                    "Vui lòng nhập tài khoản.");
            }

            if (string.IsNullOrWhiteSpace(MatKhau))
            {
                ModelState.AddModelError(
                    "MatKhau",
                    "Vui lòng nhập mật khẩu.");
            }

            if (!ModelState.IsValid)
            {
                return View();
            }

            var nhanVien = await _context.NhanViens
                .FirstOrDefaultAsync(nv =>
                    nv.TaiKhoan == TaiKhoan &&
                    nv.MatKhau == MatKhau);

            if (nhanVien == null)
            {
                ModelState.AddModelError(
                    "",
                    "Tài khoản hoặc mật khẩu không đúng.");

                return View();
            }

            if (nhanVien.TrangThai != "Hoạt động")
            {
                ModelState.AddModelError(
                    "",
                    "Tài khoản nhân viên hiện không hoạt động.");

                return View();
            }

            HttpContext.Session.SetInt32(
                "MaNV",
                nhanVien.MaNV);

            HttpContext.Session.SetString(
                "HoTen",
                nhanVien.HoTen);

            HttpContext.Session.SetString(
                "VaiTro",
                nhanVien.VaiTro);

            return RedirectToAction(
                "Index",
                "Home");
        }

        // GET: Account/Logout
        [HttpGet]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();

            return RedirectToAction(
                nameof(Login));
        }
    }
}