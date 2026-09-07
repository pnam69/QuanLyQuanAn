using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLyQuanAn.Migrations
{
    /// <inheritdoc />
    public partial class AddNhanVienAndOrderDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DonHangs_Bans_BanMaBan",
                table: "DonHangs");

            migrationBuilder.DropIndex(
                name: "IX_DonHangs_BanMaBan",
                table: "DonHangs");

            migrationBuilder.DropColumn(
                name: "BanMaBan",
                table: "DonHangs");

            migrationBuilder.CreateTable(
                name: "ChiTietDonHangs",
                columns: table => new
                {
                    MaChiTiet = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaDon = table.Column<int>(type: "int", nullable: false),
                    MaMon = table.Column<int>(type: "int", nullable: false),
                    SoLuong = table.Column<int>(type: "int", nullable: false),
                    DonGia = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ThanhTien = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChiTietDonHangs", x => x.MaChiTiet);
                    table.ForeignKey(
                        name: "FK_ChiTietDonHangs_DonHangs_MaDon",
                        column: x => x.MaDon,
                        principalTable: "DonHangs",
                        principalColumn: "MaDon",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChiTietDonHangs_MonAns_MaMon",
                        column: x => x.MaMon,
                        principalTable: "MonAns",
                        principalColumn: "MaMon",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NhanViens",
                columns: table => new
                {
                    MaNV = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HoTen = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SDT = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TaiKhoan = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MatKhau = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    VaiTro = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    TrangThai = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NhanViens", x => x.MaNV);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DonHangs_MaBan",
                table: "DonHangs",
                column: "MaBan");

            migrationBuilder.CreateIndex(
                name: "IX_DonHangs_MaNV",
                table: "DonHangs",
                column: "MaNV");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietDonHangs_MaDon",
                table: "ChiTietDonHangs",
                column: "MaDon");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietDonHangs_MaMon",
                table: "ChiTietDonHangs",
                column: "MaMon");

            migrationBuilder.AddForeignKey(
                name: "FK_DonHangs_Bans_MaBan",
                table: "DonHangs",
                column: "MaBan",
                principalTable: "Bans",
                principalColumn: "MaBan",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DonHangs_NhanViens_MaNV",
                table: "DonHangs",
                column: "MaNV",
                principalTable: "NhanViens",
                principalColumn: "MaNV",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DonHangs_Bans_MaBan",
                table: "DonHangs");

            migrationBuilder.DropForeignKey(
                name: "FK_DonHangs_NhanViens_MaNV",
                table: "DonHangs");

            migrationBuilder.DropTable(
                name: "ChiTietDonHangs");

            migrationBuilder.DropTable(
                name: "NhanViens");

            migrationBuilder.DropIndex(
                name: "IX_DonHangs_MaBan",
                table: "DonHangs");

            migrationBuilder.DropIndex(
                name: "IX_DonHangs_MaNV",
                table: "DonHangs");

            migrationBuilder.AddColumn<int>(
                name: "BanMaBan",
                table: "DonHangs",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DonHangs_BanMaBan",
                table: "DonHangs",
                column: "BanMaBan");

            migrationBuilder.AddForeignKey(
                name: "FK_DonHangs_Bans_BanMaBan",
                table: "DonHangs",
                column: "BanMaBan",
                principalTable: "Bans",
                principalColumn: "MaBan");
        }
    }
}
