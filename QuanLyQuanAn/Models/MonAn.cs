using System.ComponentModel.DataAnnotations;

namespace QuanLyQuanAn.Models
{
    public class MonAn
    {
        [Key]
        public int MaMon { get; set; }

        [Required(ErrorMessage = "Tên món không được để trống")]
        [StringLength(100, ErrorMessage = "Tên món không được vượt quá 100 ký tự")]
        [Display(Name = "Tên món")]
        public string TenMon { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn danh mục")]
        [Display(Name = "Danh mục")]
        public int MaDM { get; set; }

        [Range(0, 1000000000, ErrorMessage = "Đơn giá phải từ 0 trở lên")]
        [Display(Name = "Đơn giá")]
        public decimal DonGia { get; set; }

        [StringLength(500, ErrorMessage = "Mô tả không được vượt quá 500 ký tự")]
        [Display(Name = "Mô tả")]
        public string? MoTa { get; set; }

        [Display(Name = "Trạng thái")]
        public bool TrangThai { get; set; }

        public DanhMuc? DanhMuc { get; set; }
    }
}