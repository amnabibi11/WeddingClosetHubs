using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WeddingClosetHubs.Migrations
{
    /// <inheritdoc />
    public partial class AddCodCashHandoverTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "DeliveryCashHandedToAdmin",
                table: "Orders",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeliveryCashHandoverDate",
                table: "Orders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryCashHandoverNotes",
                table: "Orders",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeliveryCashHandedToAdmin",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DeliveryCashHandoverDate",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DeliveryCashHandoverNotes",
                table: "Orders");
        }
    }
}
