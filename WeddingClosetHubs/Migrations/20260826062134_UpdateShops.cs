using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WeddingClosetHubs.Migrations
{
    /// <inheritdoc />
    public partial class UpdateShops : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedDate",
                table: "Shops",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsApproved",
                table: "Shops",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ShopFrontPhoto",
                table: "Shops",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShopInsidePhoto",
                table: "Shops",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShopSignboardPhoto",
                table: "Shops",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApprovedDate",
                table: "Shops");

            migrationBuilder.DropColumn(
                name: "IsApproved",
                table: "Shops");

            migrationBuilder.DropColumn(
                name: "ShopFrontPhoto",
                table: "Shops");

            migrationBuilder.DropColumn(
                name: "ShopInsidePhoto",
                table: "Shops");

            migrationBuilder.DropColumn(
                name: "ShopSignboardPhoto",
                table: "Shops");
        }
    }
}
