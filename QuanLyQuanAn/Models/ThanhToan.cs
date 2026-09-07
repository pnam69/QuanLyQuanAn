using System.ComponentModel.DataAnnotations;

namespace QuanLyQuanAn.Models
{
    public class ThanhToan
    {
        [Key]
        public int MaThanhToan { get; set; }

        public int MaDon { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Số tiền không hợp lệ")]
        [Display(Name = "Số tiền")]
        public decimal SoTien { get; set; }

        [Display(Name = "Ngày thanh toán")]
        public DateTime NgayThanhToan { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "Vui lòng chọn phương thức thanh toán")]
        [StringLength(30)]
        [Display(Name = "Phương thức")]
        public string PhuongThuc { get; set; } = "Tiền mặt";

        [StringLength(30)]
        [Display(Name = "Trạng thái")]
        public string TrangThai { get; set; } = "Đã thanh toán";

        public DonHang? DonHang { get; set; }
    }
}