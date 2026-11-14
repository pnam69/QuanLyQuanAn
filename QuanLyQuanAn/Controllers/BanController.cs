using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLyQuanAn.Data;
using QuanLyQuanAn.Models;
using QuanLyQuanAn.Filters;

namespace QuanLyQuanAn.Controllers
{
    [RoleAuthorize("Admin")]
    public class BanController : Controller
    {
        private readonly AppDbContext _context;

        public BanController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? search, int? maTang)
        {
            var tangs = await _context.Tangs
                .OrderBy(x => x.MaTang)
                .ToListAsync();

            if (maTang == null && tangs.Any())
            {
                maTang = tangs.First().MaTang;
            }

            ViewBag.Tangs = new SelectList(
                tangs,
                "MaTang",
                "TenTang",
                maTang);

            ViewBag.SelectedTang = maTang;

            var query = _context.Bans
                .Include(x => x.Tang)
                .AsQueryable();

            if (maTang.HasValue)
            {
                query = query.Where(x => x.MaTang == maTang.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(x => x.SoBan.Contains(search));
            }

            var bans = await query
                .OrderBy(x => x.SoBan)
                .ToListAsync();

            ViewBag.Search = search;

            return View(bans);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadTangDropdown();

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Ban ban)
        {
            bool tangTonTai = await _context.Tangs
                .AnyAsync(x => x.MaTang == ban.MaTang);

            if (!tangTonTai)
            {
                ModelState.AddModelError(
                    "MaTang",
                    "Vui lòng chọn tầng hợp lệ.");
            }

            if (tangTonTai && await _context.Bans.AnyAsync(x =>
                x.MaTang == ban.MaTang &&
                x.SoBan == ban.SoBan))
            {
                ModelState.AddModelError(
                    "SoBan",
                    "Số bàn này đã tồn tại ở tầng đã chọn.");
            }

            if (!ModelState.IsValid)
            {
                await LoadTangDropdown(ban.MaTang);
                return View(ban);
            }

            _context.Bans.Add(ban);

            await _context.SaveChangesAsync();

            TempData["Success"] = "Thêm bàn thành công.";

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ban = await _context.Bans.FindAsync(id);

            if (ban == null)
            {
                return NotFound();
            }

            await LoadTangDropdown(ban.MaTang);

            return View(ban);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Ban ban)
        {
            if (id != ban.MaBan)
            {
                return NotFound();
            }

            bool tangTonTai = await _context.Tangs
                .AnyAsync(x => x.MaTang == ban.MaTang);

            if (!tangTonTai)
            {
                ModelState.AddModelError(
                    "MaTang",
                    "Vui lòng chọn tầng hợp lệ.");
            }

            if (tangTonTai && await _context.Bans.AnyAsync(x =>
                x.MaTang == ban.MaTang &&
                x.SoBan == ban.SoBan &&
                x.MaBan != ban.MaBan))
            {
                ModelState.AddModelError(
                    "SoBan",
                    "Số bàn này đã tồn tại ở tầng đã chọn.");
            }

            if (!ModelState.IsValid)
            {
                await LoadTangDropdown(ban.MaTang);
                return View(ban);
            }

            var existingBan = await _context.Bans
                .FirstOrDefaultAsync(x => x.MaBan == id);

            if (existingBan == null)
            {
                return NotFound();
            }

            existingBan.SoBan = ban.SoBan;
            existingBan.SoCho = ban.SoCho;
            existingBan.TrangThai = ban.TrangThai;
            existingBan.MaTang = ban.MaTang;
            existingBan.SoGhe = ban.SoGhe;
            existingBan.ViTriX = ban.ViTriX;
            existingBan.ViTriY = ban.ViTriY;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!BanExists(ban.MaBan))
                {
                    return NotFound();
                }

                throw;
            }

            TempData["Success"] = "Cập nhật bàn thành công.";

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ban = await _context.Bans
                .Include(x => x.Tang)
                .FirstOrDefaultAsync(x => x.MaBan == id);

            if (ban == null)
            {
                return NotFound();
            }

            return View(ban);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var ban = await _context.Bans
                .FirstOrDefaultAsync(x => x.MaBan == id);

            if (ban == null)
            {
                return NotFound();
            }

            if (ban.TrangThai == "Đang phục vụ")
            {
                TempData["Error"] =
                    "Không thể xóa bàn này vì đang phục vụ khách.";

                return RedirectToAction(nameof(Index));
            }

            bool dangCoDonHang = await _context.DonHangs
                .AnyAsync(x => x.MaBan == id);

            if (dangCoDonHang)
            {
                TempData["Error"] =
                    "Không thể xóa bàn này vì đã có đơn hàng liên quan.";

                return RedirectToAction(nameof(Index));
            }

            _context.Bans.Remove(ban);

            await _context.SaveChangesAsync();

            TempData["Success"] = "Xóa bàn thành công.";

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> OpenTable(int id)
        {
            var ban = await _context.Bans
                .FirstOrDefaultAsync(x => x.MaBan == id);

            if (ban == null)
            {
                return NotFound();
            }

            var maDon = await _context.DonHangs
                .Where(x =>
                    x.MaBan == id &&
                    x.TrangThai == "Đang phục vụ")
                .OrderByDescending(x => x.NgayLap)
                .Select(x => (int?)x.MaDon)
                .FirstOrDefaultAsync();

            if (maDon.HasValue)
            {
                return RedirectToAction(
                    "Details",
                    "DonHang",
                    new { id = maDon.Value });
            }

            return RedirectToAction("Edit", "Ban", new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SavePositions(
    [FromBody] List<BanPositionUpdate> positions)
        {
            if (positions == null || positions.Count == 0)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Không có vị trí nào cần lưu."
                });
            }

            if (positions.Any(x =>
                x.ViTriX < 0 ||
                x.ViTriY < 0))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Vị trí bàn không hợp lệ."
                });
            }

            var ids = positions.Select(x => x.MaBan).Distinct().ToList();

            if (ids.Count != positions.Count)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Dữ liệu bàn bị trùng."
                });
            }

            var bans = await _context.Bans
                .Where(x => ids.Contains(x.MaBan))
                .ToListAsync();

            if (bans.Count != positions.Count)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Một số bàn không tồn tại."
                });
            }

            foreach (var position in positions)
            {
                var ban = bans.First(x => x.MaBan == position.MaBan);

                ban.ViTriX = position.ViTriX;
                ban.ViTriY = position.ViTriY;
            }

            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                message = "Đã lưu sơ đồ bàn."
            });
        }
        private async Task LoadTangDropdown(int? selectedTang = null)
        {
            var tangs = await _context.Tangs
                .OrderBy(x => x.MaTang)
                .ToListAsync();

            ViewBag.MaTang = new SelectList(
                tangs,
                "MaTang",
                "TenTang",
                selectedTang);
        }

        private bool BanExists(int id)
        {
            return _context.Bans.Any(x => x.MaBan == id);
        }
    }
    public class BanPositionUpdate
    {
        public int MaBan { get; set; }
        public int ViTriX { get; set; }
        public int ViTriY { get; set; }
    }
}
