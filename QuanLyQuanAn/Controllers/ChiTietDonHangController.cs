using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyQuanAn.Data;
using QuanLyQuanAn.Models;
using QuanLyQuanAn.Filters;

namespace QuanLyQuanAn.Controllers
{
    [RoleAuthorize("Admin", "Employee")]
    public class ChiTietDonHangController : Controller
    {
        private readonly AppDbContext _context;

        public ChiTietDonHangController(AppDbContext context)
        {
            _context = context;
        }

        // GET: ChiTietDonHang/Create?maDon=1
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

            if (donHang.TrangThai == "Đã thanh toán" ||
                donHang.ThanhToan != null)
            {
                TempData["Error"] = "Đơn hàng đã thanh toán, không thể thêm món.";

                return RedirectToAction(
                    "Details",
                    "DonHang",
                    new { id = maDon });
            }

            ViewBag.DonHang = donHang;

            ViewBag.MonAns = await _context.MonAns
                .Where(m => m.TrangThai == "Đang bán")
                .OrderBy(m => m.TenMon)
                .ToListAsync();

            return View();
        }

        // POST: ChiTietDonHang/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            int MaDon,
            int MaMon,
            int SoLuong)
        {
            if (SoLuong <= 0)
            {
                TempData["Error"] = "Số lượng phải lớn hơn 0.";

                return RedirectToAction(
                    nameof(Create),
                    new { maDon = MaDon });
            }

            // Check order and payment status
            var donHang = await _context.DonHangs
                .Include(d => d.ThanhToan)
                .FirstOrDefaultAsync(d => d.MaDon == MaDon);

            if (donHang == null)
            {
                return NotFound();
            }

            if (donHang.TrangThai == "Đã thanh toán" ||
                donHang.ThanhToan != null)
            {
                TempData["Error"] = "Đơn hàng đã thanh toán, không thể thêm món.";

                return RedirectToAction(
                    "Details",
                    "DonHang",
                    new { id = MaDon });
            }

            // Get current dish price from database
            var monAn = await _context.MonAns
                .FirstOrDefaultAsync(m =>
                    m.MaMon == MaMon &&
                    m.TrangThai == "Đang bán");

            if (monAn == null)
            {
                TempData["Error"] = "Món ăn không tồn tại hoặc hiện không được bán.";

                return RedirectToAction(
                    nameof(Create),
                    new { maDon = MaDon });
            }

            // Check whether this dish already exists in the order
            var chiTiet = await _context.ChiTietDonHangs
                .FirstOrDefaultAsync(ct =>
                    ct.MaDon == MaDon &&
                    ct.MaMon == MaMon);

            if (chiTiet == null)
            {
                chiTiet = new ChiTietDonHang
                {
                    MaDon = MaDon,
                    MaMon = MaMon,
                    SoLuong = SoLuong,
                    DonGia = monAn.DonGia,
                    ThanhTien = monAn.DonGia * SoLuong
                };

                _context.ChiTietDonHangs.Add(chiTiet);
            }
            else
            {
                chiTiet.SoLuong += SoLuong;
                chiTiet.ThanhTien = chiTiet.SoLuong * chiTiet.DonGia;
            }

            // SQL trigger trg_UpdateOrderTotal recalculates TongTien
            await _context.SaveChangesAsync();

            return RedirectToAction(
                "Details",
                "DonHang",
                new { id = MaDon });
        }

        // POST: ChiTietDonHang/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var chiTiet = await _context.ChiTietDonHangs
                .Include(x => x.DonHang)
                    .ThenInclude(d => d!.ThanhToan)
                .FirstOrDefaultAsync(ct => ct.MaChiTiet == id);

            if (chiTiet == null || chiTiet.DonHang == null)
            {
                return NotFound();
            }

            var donHang = chiTiet.DonHang;
            var maDon = chiTiet.MaDon;

            if (donHang.TrangThai == "Đã thanh toán" ||
                donHang.ThanhToan != null)
            {
                TempData["Error"] = "Đơn hàng đã thanh toán, không thể xóa món.";

                return RedirectToAction(
                    "Details",
                    "DonHang",
                    new { id = maDon });
            }

            _context.ChiTietDonHangs.Remove(chiTiet);

            // SQL trigger recalculates TongTien after deletion
            await _context.SaveChangesAsync();

            return RedirectToAction(
                "Details",
                "DonHang",
                new { id = maDon });
        }
    }
}
