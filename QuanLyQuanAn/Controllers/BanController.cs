using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyQuanAn.Data;
using QuanLyQuanAn.Models;

namespace QuanLyQuanAn.Controllers
{
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
            if (!ModelState.IsValid)
            {
                return View(ban);
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
            var ban = await _context.Bans.FindAsync(id);

            if (ban != null)
            {
                _context.Bans.Remove(ban);

                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool BanExists(int id)
        {
            return _context.Bans.Any(x => x.MaBan == id);
        }
    }
}