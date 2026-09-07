using System.ComponentModel.DataAnnotations;

namespace QuanLyQuanAn.Models
{
    public class DonHang
    {
        [Key]
        public int MaDon { get; set; }

        public int MaBan { get; set; }

        public int MaNV { get; set; }

        public DateTime NgayLap { get; set; } = DateTime.Now;

        public decimal TongTien { get; set; }

        [StringLength(30)]
        public string TrangThai { get; set; } = "Đang phục vụ";

        public Ban? Ban { get; set; }

        public NhanVien? NhanVien { get; set; }

        public ICollection<ChiTietDonHang> ChiTietDonHangs { get; set; }
            = new List<ChiTietDonHang>();

        public ThanhToan? ThanhToan { get; set; }
    }
}