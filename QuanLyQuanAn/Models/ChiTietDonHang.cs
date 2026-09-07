using System.ComponentModel.DataAnnotations;

namespace QuanLyQuanAn.Models
{
    public class ChiTietDonHang
    {
        [Key]
        public int MaChiTiet { get; set; }

        public int MaDon { get; set; }

        public int MaMon { get; set; }

        [Range(1, 1000, ErrorMessage = "Số lượng phải lớn hơn 0")]
        public int SoLuong { get; set; }

        public decimal DonGia { get; set; }

        public decimal ThanhTien { get; set; }

        public DonHang? DonHang { get; set; }

        public MonAn? MonAn { get; set; }
    }
}