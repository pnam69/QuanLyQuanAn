using System.ComponentModel.DataAnnotations;

namespace QuanLyQuanAn.Models
{
    public class MapObject
    {
        [Key]
        [StringLength(100)]
        public string Id { get; set; } = string.Empty;

        public int MaTang { get; set; }

        [Required]
        [StringLength(30)]
        public string Type { get; set; } = string.Empty;

        [StringLength(100)]
        public string Label { get; set; } = string.Empty;

        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public double Rotation { get; set; }

        public Tang? Tang { get; set; }
    }
}
