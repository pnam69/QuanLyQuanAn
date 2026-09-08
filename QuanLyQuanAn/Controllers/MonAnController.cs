using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLyQuanAn.Data;
using QuanLyQuanAn.Models;

namespace QuanLyQuanAn.Controllers
{
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
            var monAn = await _context.MonAns.FindAsync(id);

            if (monAn != null)
            {
                _context.MonAns.Remove(monAn);

                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}