using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLyQuanAn.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDatabaseModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_NhanViens_MaNV",
                table: "NhanViens",
                column: "MaNV",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MonAns_MaMon",
                table: "MonAns",
                column: "MaMon",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DonHangs_MaDon",
                table: "DonHangs",
                column: "MaDon",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DanhMucs_MaDM",
                table: "DanhMucs",
                column: "MaDM",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_NhanViens_MaNV",
                table: "NhanViens");

            migrationBuilder.DropIndex(
                name: "IX_MonAns_MaMon",
                table: "MonAns");

            migrationBuilder.DropIndex(
                name: "IX_DonHangs_MaDon",
                table: "DonHangs");

            migrationBuilder.DropIndex(
                name: "IX_DanhMucs_MaDM",
                table: "DanhMucs");
        }
    }
}
