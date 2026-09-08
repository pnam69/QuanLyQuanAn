using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyQuanAn.Data;
using QuanLyQuanAn.Models;

namespace QuanLyQuanAn.Controllers
{
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
                .FirstOrDefaultAsync(d => d.MaDon == maDon);

            if (donHang == null)
            {
                return NotFound();
            }

            if (donHang.TrangThai == "Đã thanh toán")
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

            var donHang = await _context.DonHangs
                .FirstOrDefaultAsync(d => d.MaDon == MaDon);

            if (donHang == null)
            {
                return NotFound();
            }

            if (donHang.TrangThai == "Đã thanh toán")
            {
                TempData["Error"] = "Đơn hàng đã thanh toán.";

                return RedirectToAction(
                    "Details",
                    "DonHang",
                    new { id = MaDon });
            }

            var monAn = await _context.MonAns
                .FirstOrDefaultAsync(m => m.MaMon == MaMon);

            if (monAn == null)
            {
                TempData["Error"] = "Món ăn không tồn tại.";

                return RedirectToAction(
                    nameof(Create),
                    new { maDon = MaDon });
            }

            if (monAn.TrangThai != "Đang bán")
            {
                TempData["Error"] = "Món ăn hiện không được bán.";

                return RedirectToAction(
                    nameof(Create),
                    new { maDon = MaDon });
            }

            // Kiểm tra món đã có trong đơn chưa
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

                    // Lưu giá tại thời điểm tạo đơn
                    DonGia = monAn.DonGia,

                    ThanhTien = monAn.DonGia * SoLuong
                };

                _context.ChiTietDonHangs.Add(chiTiet);
            }
            else
            {
                // Nếu món đã tồn tại thì cộng thêm số lượng
                chiTiet.SoLuong += SoLuong;

                chiTiet.ThanhTien =
                    chiTiet.SoLuong * chiTiet.DonGia;
            }

            await _context.SaveChangesAsync();

            // Tính lại tổng tiền của đơn
            donHang.TongTien = await _context.ChiTietDonHangs
                .Where(ct => ct.MaDon == MaDon)
                .SumAsync(ct => ct.ThanhTien);

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
                .FirstOrDefaultAsync(ct => ct.MaChiTiet == id);

            if (chiTiet == null)
            {
                return NotFound();
            }

            var maDon = chiTiet.MaDon;

            var donHang = await _context.DonHangs
                .FirstOrDefaultAsync(d => d.MaDon == maDon);

            if (donHang == null)
            {
                return NotFound();
            }

            if (donHang.TrangThai == "Đã thanh toán")
            {
                TempData["Error"] =
                    "Đơn hàng đã thanh toán, không thể xóa món.";

                return RedirectToAction(
                    "Details",
                    "DonHang",
                    new { id = maDon });
            }

            _context.ChiTietDonHangs.Remove(chiTiet);

            await _context.SaveChangesAsync();

            // Cập nhật lại tổng tiền
            donHang.TongTien = await _context.ChiTietDonHangs
                .Where(ct => ct.MaDon == maDon)
                .SumAsync(ct => ct.ThanhTien);

            await _context.SaveChangesAsync();

            return RedirectToAction(
                "Details",
                "DonHang",
                new { id = maDon });
        }
    }
}