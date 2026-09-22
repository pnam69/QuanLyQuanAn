using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyQuanAn.Data;
using QuanLyQuanAn.Filters;
using QuanLyQuanAn.Models;

namespace QuanLyQuanAn.Controllers
{
    [RoleAuthorize("Admin", "Employee")]
    public class DonHangController : Controller
    {
        private readonly AppDbContext _context;

        public DonHangController(AppDbContext context)
        {
            _context = context;
        }

        // GET: DonHang
        public async Task<IActionResult> Index(string? search)
        {
            var query = _context.DonHangs
                .Include(d => d.Ban)
                .Include(d => d.NhanVien)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                if (int.TryParse(search, out int maDon))
                {
                    query = query.Where(d =>
                        d.MaDon == maDon ||
                        d.Ban!.SoBan.Contains(search) ||
                        d.NhanVien!.HoTen.Contains(search) ||
                        d.TrangThai.Contains(search));
                }
                else
                {
                    query = query.Where(d =>
                        d.Ban!.SoBan.Contains(search) ||
                        d.NhanVien!.HoTen.Contains(search) ||
                        d.TrangThai.Contains(search));
                }
            }

            var donHangs = await query
                .OrderByDescending(d => d.NgayLap)
                .ToListAsync();

            ViewBag.Search = search;

            return View(donHangs);
        }

        // GET: DonHang/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var donHang = await _context.DonHangs
                .Include(d => d.Ban)
                .Include(d => d.NhanVien)
                .Include(d => d.ChiTietDonHangs)
                    .ThenInclude(ct => ct.MonAn)
                .Include(d => d.ThanhToan)
                .FirstOrDefaultAsync(d => d.MaDon == id);

            if (donHang == null)
            {
                return NotFound();
            }

            return View(donHang);
        }

        // GET: DonHang/Create
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var maNV = HttpContext.Session.GetInt32("MaNV");

            if (maNV == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var nhanVien = await _context.NhanViens
                .FirstOrDefaultAsync(nv => nv.MaNV == maNV);

            if (nhanVien == null)
            {
                HttpContext.Session.Clear();
                return RedirectToAction("Login", "Account");
            }

            ViewBag.NhanVien = nhanVien;

            ViewBag.Bans = await _context.Bans
                .Where(b => b.TrangThai == "Trống")
                .OrderBy(b => b.SoBan)
                .ToListAsync();

            return View();
        }

        // POST: DonHang/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int MaBan)
        {
            var maNV = HttpContext.Session.GetInt32("MaNV");

            if (maNV == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var nhanVien = await _context.NhanViens
                .FirstOrDefaultAsync(nv =>
                    nv.MaNV == maNV &&
                    nv.TrangThai == "Hoạt động");

            if (nhanVien == null)
            {
                HttpContext.Session.Clear();

                return RedirectToAction("Login", "Account");
            }

            var ban = await _context.Bans
                .FirstOrDefaultAsync(b => b.MaBan == MaBan);

            if (ban == null)
            {
                ModelState.AddModelError(
                    "MaBan",
                    "Bàn không tồn tại.");
            }
            else if (ban.TrangThai != "Trống")
            {
                ModelState.AddModelError(
                    "MaBan",
                    "Bàn này hiện không trống.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.NhanVien = nhanVien;

                ViewBag.Bans = await _context.Bans
                    .Where(b => b.TrangThai == "Trống")
                    .OrderBy(b => b.SoBan)
                    .ToListAsync();

                return View();
            }

            var donHang = new DonHang
            {
                MaBan = MaBan,
                MaNV = maNV.Value,
                NgayLap = DateTime.Now,
                TongTien = 0,
                TrangThai = "Đang phục vụ"
            };

            _context.DonHangs.Add(donHang);

            ban.TrangThai = "Đang phục vụ";

            await _context.SaveChangesAsync();

            return RedirectToAction(
                nameof(Details),
                new { id = donHang.MaDon });
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
            {
                return NotFound();
            }

            if (donHang.TrangThai == "Đã thanh toán" || donHang.ThanhToan != null)
            {
                TempData["Error"] =
                    "Không thể xóa đơn hàng đã thanh toán.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
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