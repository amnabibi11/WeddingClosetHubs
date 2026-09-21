using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WeddingClosetHubs.Migrations
{
    /// <inheritdoc />
    public partial class AddChatImageEditDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "EditedDate",
                table: "ChatMessages",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImagePath",
                table: "ChatMessages",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "ChatMessages",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EditedDate",
                table: "ChatMessages");

            migrationBuilder.DropColumn(
                name: "ImagePath",
                table: "ChatMessages");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "ChatMessages");
        }
    }
}
