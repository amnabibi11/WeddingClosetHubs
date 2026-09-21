using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WeddingClosetHubs.Migrations
{
    /// <inheritdoc />
    public partial class AddManualPayoutTransactionFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DeliveryPaymentNotes",
                table: "Orders",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryTransactionId",
                table: "Orders",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShopkeeperPaymentNotes",
                table: "Orders",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShopkeeperTransactionId",
                table: "Orders",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeliveryPaymentNotes",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DeliveryTransactionId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ShopkeeperPaymentNotes",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ShopkeeperTransactionId",
                table: "Orders");
        }
    }
}
