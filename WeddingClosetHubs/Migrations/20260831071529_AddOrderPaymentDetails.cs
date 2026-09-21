using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WeddingClosetHubs.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderPaymentDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DeliveryPaidAmount",
                table: "Orders",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryPaymentAccount",
                table: "Orders",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryPaymentMethod",
                table: "Orders",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ShopkeeperPaidAmount",
                table: "Orders",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "ShopkeeperPaymentAccount",
                table: "Orders",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShopkeeperPaymentMethod",
                table: "Orders",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TransactionId",
                table: "Orders",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TransactionImage",
                table: "Orders",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeliveryPaidAmount",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DeliveryPaymentAccount",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DeliveryPaymentMethod",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ShopkeeperPaidAmount",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ShopkeeperPaymentAccount",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ShopkeeperPaymentMethod",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "TransactionId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "TransactionImage",
                table: "Orders");
        }
    }
}
