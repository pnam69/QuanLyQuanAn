using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyQuanAn.Data;
using QuanLyQuanAn.Models;
using QuanLyQuanAn.Filters;
namespace QuanLyQuanAn.Controllers
{
    [RoleAuthorize("Quản trị viên")]
    public class BanController : Controller
    {
        private readonly AppDbContext _context;

        public BanController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? search)
        {
            var query = _context.Bans.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(x =>
                    x.SoBan.Contains(search));
            }

            var bans = await query
                .OrderBy(x => x.MaBan)
                .ToListAsync();

            ViewBag.Search = search;

            return View(bans);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Ban ban)
        {
            if (await _context.Bans.AnyAsync(x => x.SoBan == ban.SoBan))
            {
                ModelState.AddModelError(
                    "SoBan",
                    "Số bàn này đã tồn tại.");
            }
            if (!ModelState.IsValid)
            {
                return View(ban);
            }
            if (ban.SoCho <= 0)
            {
                ModelState.AddModelError(
                    "SoCho",
                    "Số chỗ phải lớn hơn 0.");
            }
            _context.Bans.Add(ban);

            await _context.SaveChangesAsync();

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
            if (ban.SoCho <= 0)
            {
                ModelState.AddModelError(
                    "SoCho",
                    "Số chỗ phải lớn hơn 0.");
            }
            return View(ban);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Ban ban)
        {
            if (await _context.Bans.AnyAsync(x => x.SoBan == ban.SoBan && x.MaBan != ban.MaBan))
            {
                ModelState.AddModelError(
                    "SoBan",
                    "Số bàn này đã tồn tại.");
            }
            if (id != ban.MaBan)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(ban);
            }

            try
            {
                _context.Update(ban);

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
                return NotFound();

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

        private bool BanExists(int id)
        {
            return _context.Bans.Any(x => x.MaBan == id);
        }
    }
}