using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyQuanAn.Data;
using QuanLyQuanAn.Filters;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace QuanLyQuanAn.Controllers
{
    [RoleAuthorize("Admin")]
    public class ThongKeController : Controller
    {
        private readonly AppDbContext _context;

        public ThongKeController(AppDbContext context)
        {
            _context = context;
        }
        private static IContainer HeaderCell(IContainer container)
        {
            return container
                .Background(Colors.Grey.Lighten2)
                .Border(1)
                .BorderColor(Colors.Grey.Medium)
                .Padding(5)
                .DefaultTextStyle(x =>
                    x.Bold()
                     .FontSize(9));
        }

        private static IContainer BodyCell(IContainer container)
        {
            return container
                .BorderBottom(1)
                .BorderLeft(1)
                .BorderRight(1)
                .BorderColor(Colors.Grey.Lighten2)
                .Padding(5);
        }

        private static IContainer TotalLabelCell(IContainer container)
        {
            return container
                .BorderTop(1)
                .BorderBottom(1)
                .BorderColor(Colors.Grey.Medium)
                .Padding(6)
                .DefaultTextStyle(x =>
                    x.Bold()
                     .FontSize(10));
        }

        private static IContainer TotalCell(IContainer container)
        {
            return container
                .BorderTop(1)
                .BorderBottom(1)
                .BorderColor(Colors.Grey.Medium)
                .Padding(6)
                .DefaultTextStyle(x =>
                    x.Bold()
                     .FontSize(10));
        }

        public async Task<IActionResult> Index(
            DateTime? tuNgay,
            DateTime? denNgay)
        {
            DateTime startDate = tuNgay?.Date
                ?? DateTime.Today.AddDays(-30);

            DateTime endDate = denNgay?.Date
                ?? DateTime.Today;

            if (startDate > endDate)
            {
                ModelState.AddModelError(
                    "",
                    "Ngày bắt đầu không được lớn hơn ngày kết thúc.");

                startDate = endDate.AddDays(-30);
            }

            DateTime endExclusive = endDate.AddDays(1);

            var paidOrders = _context.DonHangs
                .Where(d =>
                    d.TrangThai == "Đã thanh toán" &&
                    d.NgayLap >= startDate &&
                    d.NgayLap < endExclusive);

            var tongDon = await paidOrders.CountAsync();

            var tongDoanhThu = await paidOrders
                .SumAsync(d => (decimal?)d.TongTien) ?? 0;

            var chiTietBanChay = await _context.ChiTietDonHangs
                .Include(ct => ct.MonAn)
                .Include(ct => ct.DonHang)
                .Where(ct =>
                    ct.DonHang!.TrangThai == "Đã thanh toán" &&
                    ct.DonHang.NgayLap >= startDate &&
                    ct.DonHang.NgayLap < endExclusive)
                .GroupBy(ct => new
                {
                    ct.MaMon,
                    TenMon = ct.MonAn!.TenMon
                })
                .Select(g => new
                {
                    TenMon = g.Key.TenMon,
                    SoLuong = g.Sum(x => x.SoLuong),
                    DoanhThu = g.Sum(x => x.ThanhTien)
                })
                .OrderByDescending(x => x.SoLuong)
                .Take(10)
                .ToListAsync();

            var doanhThuTheoNgay = await paidOrders
                .GroupBy(d => d.NgayLap.Date)
                .Select(g => new
                {
                    Ngay = g.Key,
                    SoDon = g.Count(),
                    DoanhThu = g.Sum(x => x.TongTien)
                })
                .OrderBy(x => x.Ngay)
                .ToListAsync();

            ViewBag.TuNgay = startDate.ToString("yyyy-MM-dd");
            ViewBag.DenNgay = endDate.ToString("yyyy-MM-dd");

            ViewBag.TongDon = tongDon;
            ViewBag.TongDoanhThu = tongDoanhThu;

            ViewBag.ChiTietBanChay = chiTietBanChay;
            ViewBag.DoanhThuTheoNgay = doanhThuTheoNgay;

            return View();
        }
        [HttpGet]
        public async Task<IActionResult> Export(DateTime? tuNgay, DateTime? denNgay)
        {
            DateTime startDate = tuNgay?.Date ?? DateTime.Today.AddDays(-30);
            DateTime endDate = denNgay?.Date ?? DateTime.Today;

            if (startDate > endDate)
            {
                TempData["Error"] =
                    "Ngày bắt đầu không được lớn hơn ngày kết thúc.";

                return RedirectToAction(nameof(Index));
            }

            DateTime endExclusive = endDate.AddDays(1);

            var orders = await _context.DonHangs
                .Include(d => d.Ban)
                .Include(d => d.NhanVien)
                .Where(d =>
                    d.TrangThai == "Đã thanh toán" &&
                    d.NgayLap >= startDate &&
                    d.NgayLap < endExclusive)
                .OrderBy(d => d.NgayLap)
                .ToListAsync();

            using var workbook = new ClosedXML.Excel.XLWorkbook();

            var worksheet = workbook.Worksheets.Add("Báo cáo doanh thu");

            // =========================
            // TIÊU ĐỀ
            // =========================

            worksheet.Cell("A1").Value = "BÁO CÁO DOANH THU";

            worksheet.Range("A1:G1").Merge();

            worksheet.Cell("A1").Style.Font.Bold = true;
            worksheet.Cell("A1").Style.Font.FontSize = 18;
            worksheet.Cell("A1").Style.Alignment.Horizontal =
                ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
            worksheet.Cell("A1").Style.Alignment.Vertical =
                ClosedXML.Excel.XLAlignmentVerticalValues.Center;

            worksheet.Row(1).Height = 30;

            // =========================
            // KHOẢNG THỜI GIAN
            // =========================

            worksheet.Cell("A2").Value =
                $"Từ ngày: {startDate:dd/MM/yyyy} - Đến ngày: {endDate:dd/MM/yyyy}";

            worksheet.Range("A2:G2").Merge();

            worksheet.Cell("A2").Style.Font.Italic = true;
            worksheet.Cell("A2").Style.Alignment.Horizontal =
                ClosedXML.Excel.XLAlignmentHorizontalValues.Center;

            // =========================
            // HEADER
            // =========================

            int headerRow = 4;

            worksheet.Cell(headerRow, 1).Value = "Mã đơn";
            worksheet.Cell(headerRow, 2).Value = "Mã bàn";
            worksheet.Cell(headerRow, 3).Value = "Số bàn";
            worksheet.Cell(headerRow, 4).Value = "Employee";
            worksheet.Cell(headerRow, 5).Value = "Ngày lập";
            worksheet.Cell(headerRow, 6).Value = "Tổng tiền";
            worksheet.Cell(headerRow, 7).Value = "Trạng thái";

            var headerRange =
                worksheet.Range(headerRow, 1, headerRow, 7);

            headerRange.Style.Font.Bold = true;

            headerRange.Style.Alignment.Horizontal =
                ClosedXML.Excel.XLAlignmentHorizontalValues.Center;

            headerRange.Style.Alignment.Vertical =
                ClosedXML.Excel.XLAlignmentVerticalValues.Center;

            // =========================
            // DATA
            // =========================

            int currentRow = headerRow + 1;

            foreach (var donHang in orders)
            {
                worksheet.Cell(currentRow, 1).Value =
                    donHang.MaDon;

                worksheet.Cell(currentRow, 2).Value =
                    donHang.MaBan;

                worksheet.Cell(currentRow, 3).Value =
                    donHang.Ban?.SoBan ?? "";

                worksheet.Cell(currentRow, 4).Value =
                    donHang.NhanVien?.HoTen ?? "";

                worksheet.Cell(currentRow, 5).Value =
                    donHang.NgayLap;

                worksheet.Cell(currentRow, 5)
                    .Style.DateFormat.Format = "dd/MM/yyyy HH:mm";

                worksheet.Cell(currentRow, 6).Value =
                    donHang.TongTien;

                worksheet.Cell(currentRow, 6)
                    .Style.NumberFormat.Format = "#,##0 \"₫\"";

                worksheet.Cell(currentRow, 7).Value =
                    donHang.TrangThai;

                currentRow++;
            }

            // =========================
            // TỔNG DOANH THU
            // =========================

            int totalRow = currentRow;

            worksheet.Cell(totalRow, 5).Value = "TỔNG DOANH THU";
            worksheet.Cell(totalRow, 5).Style.Font.Bold = true;

            worksheet.Cell(totalRow, 6).FormulaA1 =
                $"=SUM(F{headerRow + 1}:F{totalRow - 1})";

            worksheet.Cell(totalRow, 6).Style.Font.Bold = true;
            worksheet.Cell(totalRow, 6)
                .Style.NumberFormat.Format = "#,##0 \"₫\"";

            // =========================
            // BORDER
            // =========================

            var tableRange =
                worksheet.Range(
                    headerRow,
                    1,
                    Math.Max(totalRow - 1, headerRow),
                    7);

            tableRange.Style.Border.OutsideBorder =
                ClosedXML.Excel.XLBorderStyleValues.Thin;

            tableRange.Style.Border.InsideBorder =
                ClosedXML.Excel.XLBorderStyleValues.Thin;

            // =========================
            // FILTER
            // =========================

            if (orders.Count > 0)
            {
                worksheet.Range(
                    headerRow,
                    1,
                    totalRow - 1,
                    7)
                    .SetAutoFilter();
            }

            // =========================
            // CỘT
            // =========================

            worksheet.Column(1).Width = 12;
            worksheet.Column(2).Width = 12;
            worksheet.Column(3).Width = 12;
            worksheet.Column(4).Width = 25;
            worksheet.Column(5).Width = 22;
            worksheet.Column(6).Width = 20;
            worksheet.Column(7).Width = 20;

            // =========================
            // FREEZE HEADER
            // =========================

            worksheet.SheetView.FreezeRows(headerRow);

            // =========================
            // CĂN CHỈNH
            // =========================

            worksheet.Column(1).Style.Alignment.Horizontal =
                ClosedXML.Excel.XLAlignmentHorizontalValues.Center;

            worksheet.Column(2).Style.Alignment.Horizontal =
                ClosedXML.Excel.XLAlignmentHorizontalValues.Center;

            worksheet.Column(3).Style.Alignment.Horizontal =
                ClosedXML.Excel.XLAlignmentHorizontalValues.Center;

            worksheet.Column(5).Style.Alignment.Horizontal =
                ClosedXML.Excel.XLAlignmentHorizontalValues.Center;

            worksheet.Column(6).Style.Alignment.Horizontal =
                ClosedXML.Excel.XLAlignmentHorizontalValues.Right;

            worksheet.Column(7).Style.Alignment.Horizontal =
                ClosedXML.Excel.XLAlignmentHorizontalValues.Center;

            // =========================
            // XUẤT FILE
            // =========================

            using var stream = new MemoryStream();

            workbook.SaveAs(stream);

            var fileName =
                $"BaoCaoDoanhThu_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}.xlsx";

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }
        [HttpGet]
        public async Task<IActionResult> ExportPdf(
    DateTime? tuNgay,
    DateTime? denNgay)
        {
            DateTime startDate =
                tuNgay?.Date ?? DateTime.Today.AddDays(-30);

            DateTime endDate =
                denNgay?.Date ?? DateTime.Today;

            if (startDate > endDate)
            {
                TempData["Error"] =
                    "Ngày bắt đầu không được lớn hơn ngày kết thúc.";

                return RedirectToAction(nameof(Index));
            }

            DateTime endExclusive = endDate.AddDays(1);

            var orders = await _context.DonHangs
                .Include(d => d.Ban)
                .Include(d => d.NhanVien)
                .Where(d =>
                    d.TrangThai == "Đã thanh toán" &&
                    d.NgayLap >= startDate &&
                    d.NgayLap < endExclusive)
                .OrderBy(d => d.NgayLap)
                .ToListAsync();

            decimal totalRevenue = orders.Sum(d => d.TongTien);

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(30);

                    page.DefaultTextStyle(x =>
                        x.FontFamily("Arial")
                         .FontSize(10));

                    // =========================
                    // HEADER
                    // =========================

                    page.Header()
                        .Column(column =>
                        {
                            column.Item()
                                .AlignCenter()
                                .Text("BÁO CÁO DOANH THU")
                                .Bold()
                                .FontSize(20);

                            column.Item()
                                .PaddingTop(5)
                                .AlignCenter()
                                .Text(
                                    $"Từ ngày {startDate:dd/MM/yyyy} " +
                                    $"đến ngày {endDate:dd/MM/yyyy}")
                                .FontSize(11);

                            column.Item()
                                .PaddingTop(3)
                                .AlignCenter()
                                .Text(
                                    $"Ngày xuất báo cáo: " +
                                    $"{DateTime.Now:dd/MM/yyyy HH:mm}")
                                .FontSize(9);
                        });

                    // =========================
                    // CONTENT
                    // =========================

                    page.Content()
                        .PaddingTop(20)
                        .Column(column =>
                        {
                            column.Item()
                                .PaddingBottom(10)
                                .Text(
                                    $"Tổng số đơn đã thanh toán: " +
                                    $"{orders.Count}")
                                .Bold()
                                .FontSize(11);

                            column.Item()
                                .Table(table =>
                                {
                                    // -------------------------
                                    // COLUMNS
                                    // -------------------------

                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.ConstantColumn(55);
                                        columns.ConstantColumn(55);
                                        columns.ConstantColumn(55);
                                        columns.RelativeColumn(2);
                                        columns.ConstantColumn(105);
                                        columns.ConstantColumn(100);
                                        columns.ConstantColumn(90);
                                    });

                                    // -------------------------
                                    // HEADER
                                    // -------------------------

                                    table.Header(header =>
                                    {
                                        header.Cell()
                                            .Element(HeaderCell)
                                            .Text("Mã đơn");

                                        header.Cell()
                                            .Element(HeaderCell)
                                            .Text("Mã bàn");

                                        header.Cell()
                                            .Element(HeaderCell)
                                            .Text("Số bàn");

                                        header.Cell()
                                            .Element(HeaderCell)
                                            .Text("Employee");

                                        header.Cell()
                                            .Element(HeaderCell)
                                            .Text("Ngày lập");

                                        header.Cell()
                                            .Element(HeaderCell)
                                            .AlignRight()
                                            .Text("Tổng tiền");

                                        header.Cell()
                                            .Element(HeaderCell)
                                            .Text("Trạng thái");
                                    });

                                    // -------------------------
                                    // DATA
                                    // -------------------------

                                    foreach (var order in orders)
                                    {
                                        table.Cell()
                                            .Element(BodyCell)
                                            .AlignCenter()
                                            .Text(order.MaDon.ToString());

                                        table.Cell()
                                            .Element(BodyCell)
                                            .AlignCenter()
                                            .Text(order.MaBan.ToString());

                                        table.Cell()
                                            .Element(BodyCell)
                                            .AlignCenter()
                                            .Text(
                                                order.Ban?.SoBan ?? "");

                                        table.Cell()
                                            .Element(BodyCell)
                                            .Text(
                                                order.NhanVien?.HoTen ?? "");

                                        table.Cell()
                                            .Element(BodyCell)
                                            .AlignCenter()
                                            .Text(
                                                order.NgayLap
                                                    .ToString(
                                                        "dd/MM/yyyy HH:mm"));

                                        table.Cell()
                                            .Element(BodyCell)
                                            .AlignRight()
                                            .Text(
                                                $"{order.TongTien:N0} ₫");

                                        table.Cell()
                                            .Element(BodyCell)
                                            .AlignCenter()
                                            .Text(order.TrangThai);
                                    }

                                    // -------------------------
                                    // TOTAL
                                    // -------------------------

                                    table.Cell()
                                        .ColumnSpan(5)
                                        .Element(TotalLabelCell)
                                        .AlignRight()
                                        .Text("TỔNG DOANH THU");

                                    table.Cell()
                                        .Element(TotalCell)
                                        .AlignRight()
                                        .Text(
                                            $"{totalRevenue:N0} ₫");

                                    table.Cell()
                                        .Element(TotalCell)
                                        .Text("");
                                });
                        });

                    // =========================
                    // FOOTER
                    // =========================

                    page.Footer()
                        .AlignCenter()
                        .Text(text =>
                        {
                            text.Span("Trang ");
                            text.CurrentPageNumber();
                            text.Span(" / ");
                            text.TotalPages();
                        });
                });
            });

            byte[] pdf = document.GeneratePdf();

            string fileName =
                $"BaoCaoDoanhThu_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}.pdf";

            return File(
                pdf,
                "application/pdf",
                fileName);
        }
    }


}