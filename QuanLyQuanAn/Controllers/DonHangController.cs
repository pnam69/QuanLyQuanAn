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
                .Include(d => d.ThanhToan)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                bool isTakeawaySearch =
                    search.Equals("takeaway", StringComparison.OrdinalIgnoreCase);

                int.TryParse(search, out int maDon);

                query = query.Where(d =>
                    (maDon > 0 && d.MaDon == maDon) ||
                    (d.Ban != null && d.Ban.SoBan.Contains(search)) ||
                    (d.NhanVien != null && d.NhanVien.HoTen.Contains(search)) ||
                    d.TrangThai.Contains(search) ||
                    (isTakeawaySearch && d.MaBan == null)
                );
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

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            // Lock this table until the transaction finishes.
            var ban = await _context.Bans
                .FromSqlInterpolated($@"
            SELECT * FROM Bans WITH (UPDLOCK, HOLDLOCK)
            WHERE MaBan = {MaBan}")
                .FirstOrDefaultAsync();

            if (ban == null)
            {
                ModelState.AddModelError("MaBan", "Bàn không tồn tại.");
            }
            else
            {
                // Check both the table status and existing active orders.
                bool hasActiveOrder = await _context.DonHangs.AnyAsync(d =>
                    d.MaBan == MaBan &&
                    d.TrangThai == "Đang phục vụ" &&
                    d.ThanhToan == null);

                if (ban.TrangThai != "Trống" || hasActiveOrder)
                {
                    ModelState.AddModelError(
                        "MaBan",
                        "Bàn này hiện không trống hoặc đã có đơn hàng chưa thanh toán.");
                }
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
            ban!.TrangThai = "Đang phục vụ";

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return RedirectToAction(
                nameof(Details),
                new { id = donHang.MaDon });
        }

        [HttpGet]
        public async Task<IActionResult> CreateTakeaway()
        {
            var maNV = HttpContext.Session.GetInt32("MaNV");

            if (maNV == null)
                return RedirectToAction("Login", "Account");

            var nhanVien = await _context.NhanViens
                .FirstOrDefaultAsync(nv =>
                    nv.MaNV == maNV &&
                    nv.TrangThai == "Hoạt động");

            if (nhanVien == null)
            {
                HttpContext.Session.Clear();
                return RedirectToAction("Login", "Account");
            }

            ViewBag.NhanVien = nhanVien;

            return View();
        }
        // POST: DonHang/CreateTakeaway
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("CreateTakeaway")]
        public async Task<IActionResult> CreateTakeawayPost()
        {
            var maNV = HttpContext.Session.GetInt32("MaNV");

            if (maNV == null)
                return RedirectToAction("Login", "Account");

            var nhanVien = await _context.NhanViens
                .FirstOrDefaultAsync(nv =>
                    nv.MaNV == maNV &&
                    nv.TrangThai == "Hoạt động");

            if (nhanVien == null)
            {
                HttpContext.Session.Clear();
                return RedirectToAction("Login", "Account");
            }

            var donHang = new DonHang
            {
                MaBan = null,
                MaNV = maNV.Value,
                NgayLap = DateTime.Now,
                TongTien = 0,
                TrangThai = "Đang phục vụ"
            };

            _context.DonHangs.Add(donHang);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = donHang.MaDon });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            // First retrieve the table ID.
            var orderInfo = await _context.DonHangs
                .AsNoTracking()
                .Where(d => d.MaDon == id)
                .Select(d => new { d.MaDon, d.MaBan })
                .FirstOrDefaultAsync();

            if (orderInfo == null)
            {
                return NotFound();
            }

            Ban? ban = null;

            // Lock the table before changing its status.
            if (orderInfo.MaBan.HasValue)
            {
                ban = await _context.Bans
                    .FromSqlInterpolated($@"
                SELECT * FROM Bans WITH (UPDLOCK, HOLDLOCK)
                WHERE MaBan = {orderInfo.MaBan.Value}")
                    .FirstOrDefaultAsync();
            }

            // Lock and reload the order.
            var donHang = await _context.DonHangs
                .FromSqlInterpolated($@"
            SELECT * FROM DonHangs WITH (UPDLOCK, HOLDLOCK)
            WHERE MaDon = {id}")
                .Include(d => d.ChiTietDonHangs)
                .Include(d => d.ThanhToan)
                .FirstOrDefaultAsync();

            if (donHang == null)
            {
                return NotFound();
            }

            if (donHang.TrangThai == "Đã thanh toán" ||
                donHang.ThanhToan != null)
            {
                TempData["Error"] =
                    "Không thể xóa đơn hàng đã thanh toán.";

                return RedirectToAction(nameof(Details), new { id });
            }

            // Only release the table if no other active unpaid order exists.
            if (donHang.MaBan.HasValue && ban != null)
            {
                bool hasOtherActiveOrder = await _context.DonHangs.AnyAsync(d =>
                    d.MaBan == donHang.MaBan &&
                    d.MaDon != id &&
                    d.TrangThai == "Đang phục vụ" &&
                    d.ThanhToan == null);

                if (!hasOtherActiveOrder)
                {
                    ban.TrangThai = "Trống";
                }
            }

            _context.DonHangs.Remove(donHang);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}