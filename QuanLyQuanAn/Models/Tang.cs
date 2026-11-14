using System.ComponentModel.DataAnnotations;

namespace QuanLyQuanAn.Models
{
    public class Tang
    {
        [Key]
        public int MaTang { get; set; }

        [Required(ErrorMessage = "Tên tầng không được để trống")]
        [StringLength(100)]
        [Display(Name = "Tên tầng")]
        public string TenTang { get; set; } = string.Empty;

        public ICollection<Ban> Bans { get; set; } = new List<Ban>();
    }
}
