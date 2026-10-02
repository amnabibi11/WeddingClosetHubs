using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WeddingClosetHubs.Migrations
{
    /// <inheritdoc />
    public partial class AddShopkeeperVerification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CNIC",
                table: "Users",
                type: "nvarchar(15)",
                maxLength: 15,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CNICBackImage",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CNICFrontImage",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AdminNotes",
                table: "Shops",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "Shops",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShopProofDocument",
                table: "Shops",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShopProofType",
                table: "Shops",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VerificationStatus",
                table: "Shops",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "VerificationSubmittedDate",
                table: "Shops",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "VerifiedDate",
                table: "Shops",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CNIC",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CNICBackImage",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CNICFrontImage",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "AdminNotes",
                table: "Shops");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "Shops");

            migrationBuilder.DropColumn(
                name: "ShopProofDocument",
                table: "Shops");

            migrationBuilder.DropColumn(
                name: "ShopProofType",
                table: "Shops");

            migrationBuilder.DropColumn(
                name: "VerificationStatus",
                table: "Shops");

            migrationBuilder.DropColumn(
                name: "VerificationSubmittedDate",
                table: "Shops");

            migrationBuilder.DropColumn(
                name: "VerifiedDate",
                table: "Shops");
        }
    }
}
