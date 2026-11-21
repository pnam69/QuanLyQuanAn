using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyQuanAn.Data;
using QuanLyQuanAn.Models;
using QuanLyQuanAn.Filters;

namespace QuanLyQuanAn.Controllers
{
    [RoleAuthorize("Admin", "Employee")]
    public class ThanhToanController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public ThanhToanController(
            AppDbContext context,
            IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // GET: ThanhToan/Create?maDon=1
        [HttpGet]
        public async Task<IActionResult> Create(int maDon)
        {
            var donHang = await _context.DonHangs
                .Include(d => d.Ban)
                .Include(d => d.ThanhToan)
                .FirstOrDefaultAsync(d => d.MaDon == maDon);

            if (donHang == null)
                return NotFound();

            if (donHang.TrangThai == "Đã thanh toán")
            {
                TempData["Error"] = "Đơn hàng này đã được thanh toán.";
                return RedirectToAction("Details", "DonHang",
                    new { id = maDon });
            }

            if (donHang.ThanhToan != null)
            {
                if (donHang.ThanhToan.TrangThai == "Chờ thanh toán")
                {
                    return RedirectToAction(nameof(BankQr),
                        new { maDon });
                }

                TempData["Error"] = "Đơn hàng đã có giao dịch thanh toán.";
                return RedirectToAction("Details", "DonHang",
                    new { id = maDon });
            }

            if (donHang.TongTien <= 0)
            {
                TempData["Error"] =
                    "Đơn hàng chưa có món ăn, không thể thanh toán.";

                return RedirectToAction("Details", "DonHang",
                    new { id = maDon });
            }

            ViewBag.DonHang = donHang;
            return View();
        }

        // POST: ThanhToan/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int MaDon, string PhuongThuc)
        {
            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            var donHang = await _context.DonHangs
                .FromSqlInterpolated($@"
            SELECT * FROM DonHangs WITH (UPDLOCK, HOLDLOCK)
            WHERE MaDon = {MaDon}")
                .Include(d => d.Ban)
                .Include(d => d.ThanhToan)
                .FirstOrDefaultAsync();

            if (donHang == null)
                return NotFound();

            if (donHang.TrangThai == "Đã thanh toán" ||
                donHang.ThanhToan != null)
            {
                await transaction.RollbackAsync();

                TempData["Error"] = "Đơn hàng đã có giao dịch thanh toán.";

                return RedirectToAction("Details", "DonHang",
                    new { id = MaDon });
            }

            if (donHang.TongTien <= 0)
            {
                await transaction.RollbackAsync();

                TempData["Error"] = "Đơn hàng chưa có món ăn.";

                return RedirectToAction("Details", "DonHang",
                    new { id = MaDon });
            }

            if (PhuongThuc != "Tiền mặt" &&
                PhuongThuc != "Chuyển khoản BIDV")
            {
                await transaction.RollbackAsync();

                TempData["Error"] = "Phương thức thanh toán không hợp lệ.";

                return RedirectToAction(nameof(Create),
                    new { maDon = MaDon });
            }

            bool isCash = PhuongThuc == "Tiền mặt";

            var thanhToan = new ThanhToan
            {
                MaDon = MaDon,
                SoTien = donHang.TongTien,
                NgayThanhToan = DateTime.Now,
                PhuongThuc = PhuongThuc,
                TrangThai = isCash ? "Đã thanh toán" : "Chờ thanh toán"
            };

            _context.ThanhToans.Add(thanhToan);

            if (isCash)
            {
                CompletePayment(donHang);
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            if (!isCash)
            {
                return RedirectToAction(nameof(BankQr),
                    new { maDon = MaDon });
            }

            return RedirectToAction("Details", "DonHang",
                new { id = MaDon });
        }

        // GET: ThanhToan/BankQr?maDon=1
        [HttpGet]
        public async Task<IActionResult> BankQr(int maDon)
        {
            var donHang = await _context.DonHangs
                .Include(d => d.Ban)
                .Include(d => d.ThanhToan)
                .FirstOrDefaultAsync(d => d.MaDon == maDon);

            if (donHang == null)
                return NotFound();

            if (donHang.TrangThai == "Đã thanh toán")
            {
                return RedirectToAction("Details", "DonHang",
                    new { id = maDon });
            }

            if (donHang.ThanhToan == null ||
                donHang.ThanhToan.PhuongThuc != "Chuyển khoản BIDV" ||
                donHang.ThanhToan.TrangThai != "Chờ thanh toán")
            {
                return RedirectToAction(nameof(Create),
                    new { maDon });
            }

            // BIDV CONFIGURATION - CHANGE IN appsettings.json
            string bankBin =
                _configuration["BIDV:BankBin"] ?? "970418";

            string accountNumber =
                _configuration["BIDV:AccountNumber"]
                ?? "YOUR_BIDV_ACCOUNT_NUMBER";

            string accountName =
                _configuration["BIDV:AccountName"]
                ?? "YOUR_ACCOUNT_HOLDER_NAME";

            string template =
                _configuration["BIDV:QrTemplate"] ?? "compact2";

            string transferContent = $"THANH TOAN DH{maDon}";

            string qrUrl =
                $"https://img.vietqr.io/image/{bankBin}-{accountNumber}-{template}.png" +
                $"?amount={decimal.ToInt64(decimal.Round(donHang.TongTien, 0, MidpointRounding.AwayFromZero))}" +
                $"&addInfo={Uri.EscapeDataString(transferContent)}" +
                $"&accountName={Uri.EscapeDataString(accountName)}";

            ViewBag.QrUrl = qrUrl;
            ViewBag.TransferContent = transferContent;
            ViewBag.AccountNumber = accountNumber;
            ViewBag.AccountName = accountName;
            return View(donHang);
        }

        private void CompletePayment(DonHang donHang)
        {
            donHang.TrangThai = "Đã thanh toán";

            if (donHang.Ban != null)
            {
                donHang.Ban.TrangThai = "Trống";
            }
        }
    }
}
