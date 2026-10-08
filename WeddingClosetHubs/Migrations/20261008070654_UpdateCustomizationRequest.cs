using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WeddingClosetHubs.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCustomizationRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CustomizationRequests_Products_ProductId",
                table: "CustomizationRequests");

            migrationBuilder.DropIndex(
                name: "IX_CustomizationRequests_ProductId",
                table: "CustomizationRequests");

            migrationBuilder.DropColumn(
                name: "ProductId",
                table: "CustomizationRequests");

            migrationBuilder.RenameColumn(
                name: "SelectedSize",
                table: "CustomizationRequests",
                newName: "Color");

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "CustomizationRequests",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ProductName",
                table: "CustomizationRequests",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Category",
                table: "CustomizationRequests");

            migrationBuilder.DropColumn(
                name: "ProductName",
                table: "CustomizationRequests");

            migrationBuilder.RenameColumn(
                name: "Color",
                table: "CustomizationRequests",
                newName: "SelectedSize");

            migrationBuilder.AddColumn<int>(
                name: "ProductId",
                table: "CustomizationRequests",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_CustomizationRequests_ProductId",
                table: "CustomizationRequests",
                column: "ProductId");

            migrationBuilder.AddForeignKey(
                name: "FK_CustomizationRequests_Products_ProductId",
                table: "CustomizationRequests",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "ProductId");
        }
    }
}
