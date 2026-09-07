using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLyQuanAn.Migrations
{
    /// <inheritdoc />
    public partial class MakeSoBanUnique : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Bans_SoBan",
                table: "Bans",
                column: "SoBan",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Bans_SoBan",
                table: "Bans");
        }
    }
}
