using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WeddingClosetHubs.Migrations
{
    /// <inheritdoc />
    public partial class AddRentalReturnAndRefundTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "RefundableSecurity",
                table: "Orders",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "DressReturnedDate",
                table: "OrderDetails",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RentalReturnStatus",
                table: "OrderDetails",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SecurityRefundAccount",
                table: "OrderDetails",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SecurityRefundDate",
                table: "OrderDetails",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SecurityRefundMethod",
                table: "OrderDetails",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SecurityRefundNotes",
                table: "OrderDetails",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SecurityRefundTransactionId",
                table: "OrderDetails",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "SecurityRefunded",
                table: "OrderDetails",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RefundableSecurity",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DressReturnedDate",
                table: "OrderDetails");

            migrationBuilder.DropColumn(
                name: "RentalReturnStatus",
                table: "OrderDetails");

            migrationBuilder.DropColumn(
                name: "SecurityRefundAccount",
                table: "OrderDetails");

            migrationBuilder.DropColumn(
                name: "SecurityRefundDate",
                table: "OrderDetails");

            migrationBuilder.DropColumn(
                name: "SecurityRefundMethod",
                table: "OrderDetails");

            migrationBuilder.DropColumn(
                name: "SecurityRefundNotes",
                table: "OrderDetails");

            migrationBuilder.DropColumn(
                name: "SecurityRefundTransactionId",
                table: "OrderDetails");

            migrationBuilder.DropColumn(
                name: "SecurityRefunded",
                table: "OrderDetails");
        }
    }
}
