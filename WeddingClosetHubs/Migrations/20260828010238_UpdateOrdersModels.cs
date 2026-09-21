using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WeddingClosetHubs.Migrations
{
    /// <inheritdoc />
    public partial class UpdateOrdersModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DeliveryCollectedAmount",
                table: "Orders",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeliveryCollectionDate",
                table: "Orders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeliveryPaymentDate",
                table: "Orders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryPaymentStatus",
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
                name: "DeliveryCollectedAmount",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DeliveryCollectionDate",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DeliveryPaymentDate",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DeliveryPaymentStatus",
                table: "Orders");
        }
    }
}
