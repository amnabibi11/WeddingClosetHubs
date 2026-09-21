using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WeddingClosetHubs.Migrations
{
    /// <inheritdoc />
    public partial class UpdateShopsModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Phone",
                table: "Shops",
                newName: "ShopPhone");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "Shops",
                newName: "ShopDescription");

            migrationBuilder.RenameColumn(
                name: "Address",
                table: "Shops",
                newName: "ShopAddress");

            migrationBuilder.AddColumn<string>(
                name: "ShopCategory",
                table: "Shops",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ShopCategory",
                table: "Shops");

            migrationBuilder.RenameColumn(
                name: "ShopPhone",
                table: "Shops",
                newName: "Phone");

            migrationBuilder.RenameColumn(
                name: "ShopDescription",
                table: "Shops",
                newName: "Description");

            migrationBuilder.RenameColumn(
                name: "ShopAddress",
                table: "Shops",
                newName: "Address");
        }
    }
}
