using System.ComponentModel.DataAnnotations;

namespace QuanLyQuanAn.Models
{
    public class PasswordResetToken
    {
        public int Id { get; set; }

        public int MaNV { get; set; }

        [Required]
        [StringLength(64)]
        public string TokenHash { get; set; } = string.Empty;

        public DateTime ExpiresAt { get; set; }

        public bool IsUsed { get; set; }

        public DateTime CreatedAt { get; set; }

        public NhanVien? NhanVien { get; set; }
    }
}
