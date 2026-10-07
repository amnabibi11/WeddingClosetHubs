using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WeddingClosetHubs.Migrations
{
    /// <inheritdoc />
    public partial class UpdateShopandProductModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsCustomizable",
                table: "Products");

            migrationBuilder.AddColumn<bool>(
                name: "OffersBuy",
                table: "Shops",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "OffersCustomization",
                table: "Shops",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "OffersRent",
                table: "Shops",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ShopPurchaseTypes",
                table: "Shops",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OffersBuy",
                table: "Shops");

            migrationBuilder.DropColumn(
                name: "OffersCustomization",
                table: "Shops");

            migrationBuilder.DropColumn(
                name: "OffersRent",
                table: "Shops");

            migrationBuilder.DropColumn(
                name: "ShopPurchaseTypes",
                table: "Shops");

            migrationBuilder.AddColumn<bool>(
                name: "IsCustomizable",
                table: "Products",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
