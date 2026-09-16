using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyQuanAn.Data;
using QuanLyQuanAn.Models;
using QuanLyQuanAn.Filters;

namespace QuanLyQuanAn.Controllers
{
    [RoleAuthorize("Quản trị viên")]
    public class DanhMucController : Controller
    {
        private readonly AppDbContext _context;

        public DanhMucController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? search)
        {
            var query = _context.DanhMucs.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(x => x.TenDM.Contains(search));
            }

            var danhMucs = await query.ToListAsync();

            ViewBag.Search = search;

            return View(danhMucs);
        }
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DanhMuc danhMuc)
        {

            if (await _context.DanhMucs.AnyAsync(x => x.MaDM == danhMuc.MaDM))
            {
                ModelState.AddModelError(
                    "MaDM",
                    "Danh mục này đã tồn tại.");
            }

            if (!ModelState.IsValid)
            {
                return View(danhMuc);
            }

            _context.DanhMucs.Add(danhMuc);
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

            var danhMuc = await _context.DanhMucs.FindAsync(id);

            if (danhMuc == null)
            {
                return NotFound();
            }

            if (await _context.DanhMucs.AnyAsync(x =>
    x.TenDM == danhMuc.TenDM &&
    x.MaDM != danhMuc.MaDM))
            {
                ModelState.AddModelError(
                    "TenDM",
                    "Tên danh mục này đã tồn tại.");
            }

            return View(danhMuc);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, DanhMuc danhMuc)
        {
            if (id != danhMuc.MaDM)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(danhMuc);
            }

            try
            {
                _context.Update(danhMuc);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!DanhMucExists(danhMuc.MaDM))
                {
                    return NotFound();
                }

                throw;
            }

            return RedirectToAction(nameof(Index));
        }

        private bool DanhMucExists(int id)
        {
            return _context.DanhMucs.Any(e => e.MaDM == id);
        }
        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var danhMuc = await _context.DanhMucs
                .FirstOrDefaultAsync(m => m.MaDM == id);

            if (danhMuc == null)
            {
                return NotFound();
            }

            return View(danhMuc);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var danhMuc = await _context.DanhMucs
                .FirstOrDefaultAsync(x => x.MaDM == id);

            if (danhMuc == null)
                return NotFound();

            bool dangCoMonAn = await _context.MonAns
                .AnyAsync(x => x.MaDM == id);

            if (dangCoMonAn)
            {
                TempData["Error"] =
                    "Không thể xóa danh mục này vì vẫn còn món ăn thuộc danh mục.";

                return RedirectToAction(nameof(Index));
            }

            _context.DanhMucs.Remove(danhMuc);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Xóa danh mục thành công.";

            return RedirectToAction(nameof(Index));
        }
    }

}