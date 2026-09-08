using System.ComponentModel.DataAnnotations;

namespace QuanLyQuanAn.Models
{
    public class DanhMuc
    {
        [Key]
        public int MaDM { get; set; }

        [Required(ErrorMessage = "Tên danh mục không được để trống")]
        [StringLength(100, ErrorMessage = "Tên danh mục không được vượt quá 100 ký tự")]
        [Display(Name = "Tên danh mục")]
        public string TenDM { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Mô tả không được vượt quá 500 ký tự")]
        [Display(Name = "Mô tả")]
        public string? MoTa { get; set; }

        [Display(Name = "Trạng thái")]
        public string TrangThai { get; set; } = "Hoạt động";

        public ICollection<MonAn> MonAns { get; set; } = new List<MonAn>();
    }
}