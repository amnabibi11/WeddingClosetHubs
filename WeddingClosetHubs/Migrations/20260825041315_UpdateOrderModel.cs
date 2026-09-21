using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WeddingClosetHubs.Migrations
{
    /// <inheritdoc />
    public partial class UpdateOrderModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DeliveryCharges",
                table: "Orders",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "PaymentDate",
                table: "Orders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PaymentReceived",
                table: "Orders",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "ProductTotal",
                table: "Orders",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ServiceFee",
                table: "Orders",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ShopkeeperAmount",
                table: "Orders",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "ShopkeeperPaymentStatus",
                table: "Orders",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeliveryCharges",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PaymentDate",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PaymentReceived",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ProductTotal",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ServiceFee",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ShopkeeperAmount",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ShopkeeperPaymentStatus",
                table: "Orders");
        }
    }
}
