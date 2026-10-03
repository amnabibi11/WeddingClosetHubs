using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WeddingClosetHubs.Migrations
{
    /// <inheritdoc />
    public partial class AddShopkeeperCNICImages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Image",
                table: "ProductVariants");

            migrationBuilder.AddColumn<string>(
                name: "CNICBackImage",
                table: "Shops",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CNICFrontImage",
                table: "Shops",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CNICBackImage",
                table: "Shops");

            migrationBuilder.DropColumn(
                name: "CNICFrontImage",
                table: "Shops");

            migrationBuilder.AddColumn<string>(
                name: "Image",
                table: "ProductVariants",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }
    }
}
