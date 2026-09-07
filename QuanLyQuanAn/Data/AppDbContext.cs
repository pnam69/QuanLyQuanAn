using QuanLyQuanAn.Models;
using Microsoft.EntityFrameworkCore;

namespace QuanLyQuanAn.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }
        public DbSet<MonAn> MonAns { get; set; }
        public DbSet<DanhMuc> DanhMucs { get; set; }
        public DbSet<Ban> Bans { get; set; }
        public DbSet<NhanVien> NhanViens { get; set; }
        public DbSet<DonHang> DonHangs { get; set; }
        public DbSet<ChiTietDonHang> ChiTietDonHangs { get; set; }
        public DbSet<ThanhToan> ThanhToans { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<NhanVien>()
                .HasIndex(x => x.TaiKhoan)
                .IsUnique();

            modelBuilder.Entity<Ban>()
                .HasIndex(x => x.SoBan)
                .IsUnique();

            modelBuilder.Entity<MonAn>()
                .Property(x => x.DonGia)
                .HasPrecision(18, 2);

            modelBuilder.Entity<MonAn>()
                .HasOne(x => x.DanhMuc)
                .WithMany(x => x.MonAns)
                .HasForeignKey(x => x.MaDM)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DonHang>()
                .Property(x => x.TongTien)
                .HasPrecision(18, 2);

            modelBuilder.Entity<DonHang>()
                .HasOne(x => x.Ban)
                .WithMany(x => x.DonHangs)
                .HasForeignKey(x => x.MaBan)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DonHang>()
                .HasOne(x => x.NhanVien)
                .WithMany(x => x.DonHangs)
                .HasForeignKey(x => x.MaNV)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ChiTietDonHang>()
                .Property(x => x.DonGia)
                .HasPrecision(18, 2);

            modelBuilder.Entity<ChiTietDonHang>()
                .Property(x => x.ThanhTien)
                .HasPrecision(18, 2);

            modelBuilder.Entity<ChiTietDonHang>()
                .HasOne(x => x.DonHang)
                .WithMany(x => x.ChiTietDonHangs)
                .HasForeignKey(x => x.MaDon)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ChiTietDonHang>()
                .HasOne(x => x.MonAn)
                .WithMany()
                .HasForeignKey(x => x.MaMon)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ThanhToan>()
                .Property(x => x.SoTien)
                .HasPrecision(18, 2);

            modelBuilder.Entity<ThanhToan>()
                .HasOne(x => x.DonHang)
                .WithOne(x => x.ThanhToan)
                .HasForeignKey<ThanhToan>(x => x.MaDon)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
