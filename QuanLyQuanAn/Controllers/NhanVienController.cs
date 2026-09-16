using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyQuanAn.Data;
using QuanLyQuanAn.Models;
using QuanLyQuanAn.Filters;

namespace QuanLyQuanAn.Controllers
{
    [RoleAuthorize("Quản trị viên")]
    public class NhanVienController : Controller
    {
        private readonly AppDbContext _context;

        public NhanVienController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? search)
        {
            var query = _context.NhanViens.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(x =>
                    x.HoTen.Contains(search) ||
                    x.TaiKhoan.Contains(search) ||
                    x.SDT.Contains(search));
            }

            var nhanViens = await query
                .OrderBy(x => x.MaNV)
                .ToListAsync();

            ViewBag.Search = search;

            return View(nhanViens);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(NhanVien nhanVien)
        {
            if (!ModelState.IsValid)
            {
                return View(nhanVien);
            }

            bool taiKhoanExists = await _context.NhanViens
                .AnyAsync(x => x.TaiKhoan == nhanVien.TaiKhoan);

            if (taiKhoanExists)
            {
                ModelState.AddModelError(
                    "TaiKhoan",
                    "Tài khoản đã tồn tại.");

                return View(nhanVien);
            }

            _context.NhanViens.Add(nhanVien);

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


            var nhanVien = await _context.NhanViens
                .FindAsync(id);

            if (nhanVien == null)
            {
                return NotFound();
            }
            if (await _context.NhanViens.AnyAsync(x => x.TaiKhoan == nhanVien.TaiKhoan && x.MaNV != nhanVien.MaNV))
            {
                ModelState.AddModelError(
                    "TaiKhoan",
                    "Tài khoản này đã tồn tại.");
            }
            if (string.IsNullOrWhiteSpace(nhanVien.HoTen))
            {
                ModelState.AddModelError(
                    "HoTen",
                    "Vui lòng nhập họ tên.");
            }

            return View(nhanVien);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            NhanVien nhanVien)
        {
            if (id != nhanVien.MaNV)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(nhanVien);
            }

            bool taiKhoanExists = await _context.NhanViens
                .AnyAsync(x =>
                    x.TaiKhoan == nhanVien.TaiKhoan &&
                    x.MaNV != nhanVien.MaNV);

            if (taiKhoanExists)
            {
                ModelState.AddModelError(
                    "TaiKhoan",
                    "Tài khoản đã tồn tại.");

                return View(nhanVien);
            }
            if (string.IsNullOrWhiteSpace(nhanVien.HoTen))
            {
                ModelState.AddModelError(
                    "HoTen",
                    "Vui lòng nhập họ tên.");
            }

            try
            {
                _context.Update(nhanVien);

                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!NhanVienExists(nhanVien.MaNV))
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

            var nhanVien = await _context.NhanViens
                .FirstOrDefaultAsync(x => x.MaNV == id);

            if (nhanVien == null)
            {
                return NotFound();
            }

            return View(nhanVien);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var nhanVien = await _context.NhanViens
                .FirstOrDefaultAsync(x => x.MaNV == id);

            if (nhanVien == null)
                return NotFound();

            var currentMaNV = HttpContext.Session.GetInt32("MaNV");

            if (currentMaNV == id)
            {
                TempData["Error"] =
                    "Không thể xóa tài khoản nhân viên đang đăng nhập.";

                return RedirectToAction(nameof(Index));
            }

            bool dangCoDonHang = await _context.DonHangs
                .AnyAsync(x => x.MaNV == id);

            if (dangCoDonHang)
            {
                TempData["Error"] =
                    "Không thể xóa nhân viên này vì đã có đơn hàng liên quan.";

                return RedirectToAction(nameof(Index));
            }

            _context.NhanViens.Remove(nhanVien);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Xóa nhân viên thành công.";

            return RedirectToAction(nameof(Index));
        }

        private bool NhanVienExists(int id)
        {
            return _context.NhanViens
                .Any(x => x.MaNV == id);
        }
    }
}