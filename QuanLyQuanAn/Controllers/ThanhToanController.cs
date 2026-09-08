using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyQuanAn.Data;
using QuanLyQuanAn.Models;
using QuanLyQuanAn.Filters;

namespace QuanLyQuanAn.Controllers
{
    [RoleAuthorize("Quản trị viên, Nhân viên")]
    public class ThanhToanController : Controller
    {
        private readonly AppDbContext _context;

        public ThanhToanController(AppDbContext context)
        {
            _context = context;
        }

        // GET: ThanhToan/Create?maDon=1
        [HttpGet]
        public async Task<IActionResult> Create(int maDon)
        {
            var donHang = await _context.DonHangs
                .Include(d => d.Ban)
                .Include(d => d.ThanhToan)
                .FirstOrDefaultAsync(d => d.MaDon == maDon);

            if (donHang == null)
            {
                return NotFound();
            }

            if (donHang.ThanhToan != null ||
                donHang.TrangThai == "Đã thanh toán")
            {
                TempData["Error"] = "Đơn hàng này đã được thanh toán.";

                return RedirectToAction(
                    "Details",
                    "DonHang",
                    new { id = maDon });
            }

            if (donHang.TongTien <= 0)
            {
                TempData["Error"] =
                    "Đơn hàng chưa có món ăn, không thể thanh toán.";

                return RedirectToAction(
                    "Details",
                    "DonHang",
                    new { id = maDon });
            }

            ViewBag.DonHang = donHang;

            return View();
        }

        // POST: ThanhToan/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            int MaDon,
            string PhuongThuc)
        {
            var donHang = await _context.DonHangs
                .Include(d => d.Ban)
                .Include(d => d.ThanhToan)
                .FirstOrDefaultAsync(d => d.MaDon == MaDon);

            if (donHang == null)
            {
                return NotFound();
            }

            if (donHang.ThanhToan != null ||
                donHang.TrangThai == "Đã thanh toán")
            {
                TempData["Error"] =
                    "Đơn hàng này đã được thanh toán.";

                return RedirectToAction(
                    "Details",
                    "DonHang",
                    new { id = MaDon });
            }

            if (donHang.TongTien <= 0)
            {
                TempData["Error"] =
                    "Đơn hàng chưa có món ăn.";

                return RedirectToAction(
                    "Details",
                    "DonHang",
                    new { id = MaDon });
            }

            if (string.IsNullOrWhiteSpace(PhuongThuc))
            {
                TempData["Error"] =
                    "Vui lòng chọn phương thức thanh toán.";

                return RedirectToAction(
                    nameof(Create),
                    new { maDon = MaDon });
            }

            var thanhToan = new ThanhToan
            {
                MaDon = MaDon,
                SoTien = donHang.TongTien,
                NgayThanhToan = DateTime.Now,
                PhuongThuc = PhuongThuc,
                TrangThai = "Đã thanh toán"
            };

            donHang.TrangThai = "Đã thanh toán";

            if (donHang.Ban != null)
            {
                donHang.Ban.TrangThai = "Trống";
            }

            _context.ThanhToans.Add(thanhToan);

            await _context.SaveChangesAsync();

            return RedirectToAction(
                "Details",
                "DonHang",
                new { id = MaDon });
        }
    }
}