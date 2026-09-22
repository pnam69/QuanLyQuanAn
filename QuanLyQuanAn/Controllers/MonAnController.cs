using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLyQuanAn.Data;
using QuanLyQuanAn.Models;
using QuanLyQuanAn.Filters;

namespace QuanLyQuanAn.Controllers
{
    [RoleAuthorize("Admin", "Employee")]
    public class MonAnController : Controller
    {
        private readonly AppDbContext _context;

        public MonAnController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? search)
        {
            var query = _context.MonAns
                .Include(x => x.DanhMuc)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(x =>
                    x.TenMon.Contains(search));
            }

            var monAns = await query.ToListAsync();

            ViewBag.Search = search;

            return View(monAns);
        }
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.MaDM = new SelectList(
                await _context.DanhMucs
                    .Where(x => x.TrangThai == "Hoạt động")
                    .ToListAsync(),
                "MaDM",
                "TenDM");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MonAn monAn)
        {
            if (await _context.MonAns.AnyAsync(x => x.MaMon == monAn.MaMon))
            {
                ModelState.AddModelError(
                    "MaMon",
                    "Món ăn này đã tồn tại.");
            }
            if (!ModelState.IsValid)
            {
                ViewBag.MaDM = new SelectList(
                    await _context.DanhMucs
                        .Where(x => x.TrangThai == "Hoạt động")
                        .ToListAsync(),
                    "MaDM",
                    "TenDM",
                    monAn.MaDM);

                return View(monAn);
            }
            if (monAn.DonGia < 0)
            {
                ModelState.AddModelError(
                    "DonGia",
                    "Đơn giá không được nhỏ hơn 0.");
            }
            if (await _context.MonAns.AnyAsync(x => x.TenMon == monAn.TenMon))
            {
                ModelState.AddModelError(
                    "TenMon",
                    "Tên món ăn này đã tồn tại.");
            }
            _context.MonAns.Add(monAn);

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

            var monAn = await _context.MonAns.FindAsync(id);

            if (monAn == null)
            {
                return NotFound();
            }

            ViewBag.MaDM = new SelectList(
                await _context.DanhMucs
                    .Where(x => x.TrangThai == "Hoạt động")
                    .ToListAsync(),
                "MaDM",
                "TenDM",
                monAn.MaDM);

            return View(monAn);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, MonAn monAn)
        {
            if (id != monAn.MaMon)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                ViewBag.MaDM = new SelectList(
                    await _context.DanhMucs
                        .Where(x => x.TrangThai == "Hoạt động")
                        .ToListAsync(),
                    "MaDM",
                    "TenDM",
                    monAn.MaDM);

                return View(monAn);
            }

            if (string.IsNullOrWhiteSpace(monAn.TenMon))
            {
                ModelState.AddModelError(
                    "TenMon",
                    "Vui lòng nhập tên món ăn.");
            }

            if (await _context.MonAns.AnyAsync(x =>
    x.TenMon == monAn.TenMon &&
    x.MaMon != monAn.MaMon))
            {
                ModelState.AddModelError(
                    "TenMon",
                    "Tên món ăn này đã tồn tại.");
            }

            try
            {
                _context.Update(monAn);

                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!MonAnExists(monAn.MaMon))
                {
                    return NotFound();
                }

                throw;
            }

            return RedirectToAction(nameof(Index));
        }

        private bool MonAnExists(int id)
        {
            return _context.MonAns.Any(x => x.MaMon == id);
        }
        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var monAn = await _context.MonAns
                .Include(x => x.DanhMuc)
                .FirstOrDefaultAsync(x => x.MaMon == id);

            if (monAn == null)
            {
                return NotFound();
            }

            return View(monAn);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var monAn = await _context.MonAns
                .FirstOrDefaultAsync(x => x.MaMon == id);

            if (monAn == null)
                return NotFound();

            bool dangCoDonHang = await _context.ChiTietDonHangs
                .AnyAsync(x => x.MaMon == id);

            if (dangCoDonHang)
            {
                TempData["Error"] =
                    "Không thể xóa món ăn này vì món đã có trong đơn hàng.";

                return RedirectToAction(nameof(Index));
            }

            _context.MonAns.Remove(monAn);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Xóa món ăn thành công.";

            return RedirectToAction(nameof(Index));
        }
    }
}