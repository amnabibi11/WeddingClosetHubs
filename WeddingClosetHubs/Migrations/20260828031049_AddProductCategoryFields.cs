using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WeddingClosetHubs.Migrations
{
    /// <inheritdoc />
    public partial class AddProductCategoryFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SubCategory",
                table: "Products",
                newName: "Subcategory");

            migrationBuilder.RenameColumn(
                name: "Material",
                table: "Products",
                newName: "Occasion");

            migrationBuilder.AlterColumn<string>(
                name: "Size",
                table: "Products",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomMeasurements",
                table: "Products",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CustomMeasurements",
                table: "Products");

            migrationBuilder.RenameColumn(
                name: "Subcategory",
                table: "Products",
                newName: "SubCategory");

            migrationBuilder.RenameColumn(
                name: "Occasion",
                table: "Products",
                newName: "Material");

            migrationBuilder.AlterColumn<string>(
                name: "Size",
                table: "Products",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);
        }
    }
}
