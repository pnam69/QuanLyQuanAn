using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyQuanAn.Data;
using QuanLyQuanAn.Models;

namespace QuanLyQuanAn.Controllers
{
    public class DonHangController : Controller
    {
        private readonly AppDbContext _context;

        public DonHangController(AppDbContext context)
        {
            _context = context;
        }

        // GET: DonHang
        public async Task<IActionResult> Index()
        {
            var donHangs = await _context.DonHangs
                .Include(d => d.Ban)
                .Include(d => d.NhanVien)
                .OrderByDescending(d => d.NgayLap)
                .ToListAsync();

            return View(donHangs);
        }

        // GET: DonHang/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var donHang = await _context.DonHangs
                .Include(d => d.Ban)
                .Include(d => d.NhanVien)
                .Include(d => d.ChiTietDonHangs)
                    .ThenInclude(ct => ct.MonAn)
                .Include(d => d.ThanhToan)
                .FirstOrDefaultAsync(d => d.MaDon == id);

            if (donHang == null)
                return NotFound();

            return View(donHang);
        }

        // GET: DonHang/Create
        public async Task<IActionResult> Create()
        {
            ViewBag.Bans = await _context.Bans
                .Where(b => b.TrangThai == "Trống")
                .OrderBy(b => b.SoBan)
                .ToListAsync();

            ViewBag.NhanViens = await _context.NhanViens
                .Where(nv => nv.TrangThai == "Hoạt động")
                .OrderBy(nv => nv.HoTen)
                .ToListAsync();

            return View();
        }

        // POST: DonHang/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int MaBan, int MaNV)
        {
            var ban = await _context.Bans
                .FirstOrDefaultAsync(b => b.MaBan == MaBan);

            if (ban == null)
            {
                ModelState.AddModelError("MaBan", "Bàn không tồn tại.");
            }
            else if (ban.TrangThai != "Trống")
            {
                ModelState.AddModelError("MaBan", "Bàn này hiện không trống.");
            }

            var nhanVien = await _context.NhanViens
                .FirstOrDefaultAsync(nv => nv.MaNV == MaNV);

            if (nhanVien == null)
            {
                ModelState.AddModelError("MaNV", "Nhân viên không tồn tại.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Bans = await _context.Bans
                    .Where(b => b.TrangThai == "Trống")
                    .OrderBy(b => b.SoBan)
                    .ToListAsync();

                ViewBag.NhanViens = await _context.NhanViens
                    .Where(nv => nv.TrangThai == "Hoạt động")
                    .OrderBy(nv => nv.HoTen)
                    .ToListAsync();

                return View();
            }

            var donHang = new DonHang
            {
                MaBan = MaBan,
                MaNV = MaNV,
                NgayLap = DateTime.Now,
                TongTien = 0,
                TrangThai = "Đang phục vụ"
            };

            _context.DonHangs.Add(donHang);

            // Khi có đơn, bàn chuyển sang đang phục vụ

            ban.TrangThai = "Đang phục vụ";

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = donHang.MaDon });
        }

        // POST: DonHang/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var donHang = await _context.DonHangs
                .Include(d => d.ChiTietDonHangs)
                .Include(d => d.ThanhToan)
                .Include(d => d.Ban)
                .FirstOrDefaultAsync(d => d.MaDon == id);

            if (donHang == null)
                return NotFound();

            // Không cho xóa đơn đã thanh toán
            if (donHang.ThanhToan != null)
            {
                TempData["Error"] = "Không thể xóa đơn hàng đã thanh toán.";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (donHang.Ban != null)
            {
                donHang.Ban.TrangThai = "Trống";
            }

            _context.DonHangs.Remove(donHang);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}