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
            ban.TrangThai = "Trống";
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

            var activeOrders = await _context.DonHangs
                .Where(x =>
                    x.MaBan == id &&
                    x.TrangThai == "Đang phục vụ" &&
                    x.ThanhToan == null)
                .OrderByDescending(x => x.NgayLap)
                .ToListAsync();

            if (activeOrders.Count > 1)
            {
                TempData["Error"] =
                    "Bàn này đang có nhiều đơn hàng chưa thanh toán. Vui lòng kiểm tra dữ liệu.";

                return RedirectToAction(nameof(Index));
            }

            if (activeOrders.Count == 1)
            {
                return RedirectToAction(
                    "Details",
                    "DonHang",
                    new { id = activeOrders[0].MaDon });
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

        [HttpGet]
        public async Task<IActionResult> GetMapObjects(int maTang)
        {
            var tangExists = await _context.Tangs
                .AnyAsync(x => x.MaTang == maTang);

            if (!tangExists)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Tầng không tồn tại."
                });
            }

            var objects = await _context.MapObjects
                .Where(x => x.MaTang == maTang)
                .Select(x => new
                {
                    id = x.Id,
                    type = x.Type,
                    label = x.Label,
                    x = x.X,
                    y = x.Y,
                    width = x.Width,
                    height = x.Height,
                    rotation = x.Rotation
                })
                .ToListAsync();

            return Json(objects);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveMapObjects(
            [FromBody] MapObjectsSaveRequest request)
        {
            if (request == null)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Dữ liệu không hợp lệ."
                });
            }

            var tangExists = await _context.Tangs
                .AnyAsync(x => x.MaTang == request.MaTang);

            if (!tangExists)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Tầng không tồn tại."
                });
            }

            if (request.Objects == null)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Danh sách nội thất không hợp lệ."
                });
            }

            var allowedTypes = new[]
            {
                "counter",
                "window",
                "door",
                "partition",
                "plant",
                "decoration"
            };

            if (request.Objects.Any(x =>
                string.IsNullOrWhiteSpace(x.Id) ||
                (x.Label?.Length ?? 0) > 100 ||
                !allowedTypes.Contains(x.Type) ||
                x.X < 0 || x.X > 100 ||
                x.Y < 0 || x.Y > 100 ||
                x.Width < 2 || x.Width > 100 ||
                x.Height < 2 || x.Height > 100 ||
                x.Rotation < 0 || x.Rotation > 359 ||
                double.IsNaN(x.X) || double.IsInfinity(x.X) ||
                double.IsNaN(x.Y) || double.IsInfinity(x.Y) ||
                double.IsNaN(x.Width) || double.IsInfinity(x.Width) ||
                double.IsNaN(x.Height) || double.IsInfinity(x.Height) ||
                double.IsNaN(x.Rotation) || double.IsInfinity(x.Rotation)))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Thông tin nội thất không hợp lệ."
                });
            }

            if (request.Objects.Select(x => x.Id).Distinct().Count()
                != request.Objects.Count)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "ID nội thất bị trùng."
                });
            }

            //var existingObjects = await _context.MapObjects
            //    .Where(x => x.MaTang == request.MaTang)
            //    .ToListAsync();

            //_context.MapObjects.RemoveRange(existingObjects);

            //var newObjects = request.Objects.Select(x => new MapObject
            //{
            //    Id = x.Id,
            //    MaTang = request.MaTang,
            //    Type = x.Type,
            //    Label = x.Label ?? string.Empty,
            //    X = x.X,
            //    Y = x.Y,
            //    Width = x.Width,
            //    Height = x.Height,
            //    Rotation = x.Rotation
            //}).ToList();

            //await _context.MapObjects.AddRangeAsync(newObjects);
            //await _context.SaveChangesAsync();

            await using var transaction =
    await _context.Database.BeginTransactionAsync();

            try
            {
                var existingObjects = await _context.MapObjects
                    .Where(x => x.MaTang == request.MaTang)
                    .ToListAsync();

                _context.MapObjects.RemoveRange(existingObjects);

                // Delete old objects first so their IDs can be reused.
                await _context.SaveChangesAsync();

                var newObjects = request.Objects.Select(x => new MapObject
                {
                    Id = x.Id,
                    MaTang = request.MaTang,
                    Type = x.Type,
                    Label = x.Label ?? string.Empty,
                    X = x.X,
                    Y = x.Y,
                    Width = x.Width,
                    Height = x.Height,
                    Rotation = x.Rotation
                }).ToList();

                await _context.MapObjects.AddRangeAsync(newObjects);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }

            return Json(new
            {
                success = true,
                message = "Đã lưu nội thất thành công."
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


        public class MapObjectsSaveRequest
        {
            public int MaTang { get; set; }
            public List<MapObjectUpdate> Objects { get; set; } = new();
        }

        public class MapObjectUpdate
        {
            public string Id { get; set; } = string.Empty;
            public string Type { get; set; } = string.Empty;
            public string Label { get; set; } = string.Empty;
            public double X { get; set; }
            public double Y { get; set; }
            public double Width { get; set; }
            public double Height { get; set; }
            public double Rotation { get; set; }
        }

    }
    public class BanPositionUpdate
    {
        public int MaBan { get; set; }
        public int ViTriX { get; set; }
        public int ViTriY { get; set; }
    }
}
