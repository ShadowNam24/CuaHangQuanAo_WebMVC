using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CuaHangQuanAo.Migrations
{
    /// <inheritdoc />
    public partial class AddCoverImageToItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CoverImage",
                table: "Items",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CoverImage",
                table: "Items");
        }
    }
}
