using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WeddingClosetHubs.Migrations
{
    /// <inheritdoc />
    public partial class UpdateOrdersandPayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DeliveryBoyAmount",
                table: "Payments",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "DeliveryId",
                table: "Payments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeliveryPaidDate",
                table: "Payments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryPaymentStatus",
                table: "Payments",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "ShopkeeperPaymentDate",
                table: "Orders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_DeliveryId",
                table: "Payments",
                column: "DeliveryId");

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_DeliveryBoys_DeliveryId",
                table: "Payments",
                column: "DeliveryId",
                principalTable: "DeliveryBoys",
                principalColumn: "DeliveryBoyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Payments_DeliveryBoys_DeliveryId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_DeliveryId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "DeliveryBoyAmount",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "DeliveryId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "DeliveryPaidDate",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "DeliveryPaymentStatus",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "ShopkeeperPaymentDate",
                table: "Orders");
        }
    }
}
