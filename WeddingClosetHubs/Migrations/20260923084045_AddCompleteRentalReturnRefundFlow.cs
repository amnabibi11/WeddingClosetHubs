using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WeddingClosetHubs.Migrations
{
    /// <inheritdoc />
    public partial class AddCompleteRentalReturnRefundFlow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DamageDeduction",
                table: "OrderDetails",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "DressReceivedDate",
                table: "OrderDetails",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InspectionNotes",
                table: "OrderDetails",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RefundAmount",
                table: "OrderDetails",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "ReturnMethod",
                table: "OrderDetails",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReturnNotes",
                table: "OrderDetails",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReturnPickedUpDate",
                table: "OrderDetails",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReturnRequestedDate",
                table: "OrderDetails",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SecurityReadyDate",
                table: "OrderDetails",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DamageDeduction",
                table: "OrderDetails");

            migrationBuilder.DropColumn(
                name: "DressReceivedDate",
                table: "OrderDetails");

            migrationBuilder.DropColumn(
                name: "InspectionNotes",
                table: "OrderDetails");

            migrationBuilder.DropColumn(
                name: "RefundAmount",
                table: "OrderDetails");

            migrationBuilder.DropColumn(
                name: "ReturnMethod",
                table: "OrderDetails");

            migrationBuilder.DropColumn(
                name: "ReturnNotes",
                table: "OrderDetails");

            migrationBuilder.DropColumn(
                name: "ReturnPickedUpDate",
                table: "OrderDetails");

            migrationBuilder.DropColumn(
                name: "ReturnRequestedDate",
                table: "OrderDetails");

            migrationBuilder.DropColumn(
                name: "SecurityReadyDate",
                table: "OrderDetails");
        }
    }
}
