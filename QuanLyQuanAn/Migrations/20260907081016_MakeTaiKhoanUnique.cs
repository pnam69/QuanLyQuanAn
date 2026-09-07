using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLyQuanAn.Migrations
{
    /// <inheritdoc />
    public partial class MakeTaiKhoanUnique : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_NhanViens_TaiKhoan",
                table: "NhanViens",
                column: "TaiKhoan",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_NhanViens_TaiKhoan",
                table: "NhanViens");
        }
    }
}
